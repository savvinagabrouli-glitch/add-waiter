const state = {
    tableId: null,
    categories: [],
    dishes: [],
    activeCategoryId: 0,
    cart: [],
    orderId: null,
    orderStatus: null,
    currentDishId: null,
    compositions: {},
    hasRated: false,
    ratingScore: null,
    orderAcceptedNotified: false
};

const STORAGE_KEY = "guestMenuState_v1";
const ORDER_COOLDOWN_MS = 30000;
const CALL_COOLDOWN_MS = 15000;

let lastOrderSentAt = 0;
let lastCallAt = 0;

function loadStateFromStorage() {
    try {
        if (typeof data.orderAcceptedNotified === "boolean") {
            state.orderAcceptedNotified = data.orderAcceptedNotified;
        }

        const raw = window.localStorage.getItem(STORAGE_KEY);
        if (!raw) return;
        const data = JSON.parse(raw);

        if (typeof data.tableId === "number" && data.tableId > 0) {
            state.tableId = data.tableId;
        }
        if (Array.isArray(data.cart)) {
            state.cart = data.cart;
        }
        if (typeof data.orderId === "number" && data.orderId > 0) {
            state.orderId = data.orderId;
        }
        if (typeof data.orderStatus === "string") {
            state.orderStatus = data.orderStatus;
        }
        if (typeof data.hasRated === "boolean") {
            state.hasRated = data.hasRated;
        }
    } catch (e) {
        console.warn("Не удалось прочитать сохраненный заказ );", e);
    }
}

function saveStateToStorage() {
    try {
        const data = {
            tableId: state.tableId,
            cart: state.cart,
            orderId: state.orderId,
            orderStatus: state.orderStatus,
            hasRated: state.hasRated,
            orderAcceptedNotified: state.orderAcceptedNotified  
        };
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    } catch (e) {
        console.warn("Не удалось сохранить заказ );", e);
    }
}
function restoreStateFromStorage() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) return;
        const data = JSON.parse(raw);
        if (!data || typeof data !== "object") return;

        if (!state.tableId && data.tableId) {
            state.tableId = data.tableId;
        }

        if (data.orderId) state.orderId = data.orderId;
        if (Array.isArray(data.cart)) state.cart = data.cart;
        if (data.isOrderSubmitted) state.isOrderSubmitted = true;
    } catch (e) {
        console.error("Ошибка чтения состояния из localStorage:", e);
    }
}

document.addEventListener("DOMContentLoaded", () => {
    loadStateFromStorage();

    const tableIdFromUrl = getTableIdFromUrl();
    if (tableIdFromUrl) {
        if (state.tableId && state.tableId !== tableIdFromUrl) {
            state.cart = [];
            state.orderId = null;
            state.orderStatus = null;
            state.hasRated = false;
        }
        state.tableId = tableIdFromUrl;
    }

    updateTableInfo();
    updateOrderStatusUi();

    setupEventHandlers();
    initRatingControls();
    initMenu();
    initRealtime();
});

function getTableIdFromUrl() {
    const hash = window.location.hash;
    if (hash && hash.startsWith("#diningtable")) {
        const idStr = hash.replace("#diningtable", "");
        const id = parseInt(idStr, 10);
        if (!Number.isNaN(id) && id > 0) return id;
    }

    const params = new URLSearchParams(window.location.search);
    const tableIdParam = params.get("tableId");
    if (tableIdParam) {
        const id = parseInt(tableIdParam, 10);
        if (!Number.isNaN(id) && id > 0) return id;
    }

    return null;
}

function updateTableInfo() {
    const el = document.getElementById("table-info");
    if (!el) return;
    el.textContent = state.tableId ? `Стол №${state.tableId}` : "Стол не указан";
}

function orderStatusToText(status) {
    switch (status) {
        case "Preorder":
            return "Предзаказ отправлен";
        case "New":
            return "Заказ принят";
        case "Pending":
            return "Заказ готовится";
        case "ReadyToPay":
            return "Заказ готов к оплате";
        case "Closed":
            return "Заказ закрыт";
        case "Cancelled":
            return "Заказ отменён";
        default:
            return "Заказ ещё не оформлен";
    }
}

function updateOrderStatusUi() {
    const pill = document.getElementById("order-status-pill");
    const textEl = document.getElementById("order-status-text");
    if (!pill || !textEl) return;

    if (!state.orderId || !state.orderStatus) {
        pill.classList.remove("order-status-pill--visible");
        textEl.textContent = "Заказ ещё не оформлен";
        return;
    }

    textEl.textContent = orderStatusToText(state.orderStatus);
    pill.classList.add("order-status-pill--visible");
}

let toastTimeoutId = null;

function showToast(message, type = "info") {
    const toast = document.getElementById("toast");
    const messageEl = document.getElementById("toast-message");
    if (!toast || !messageEl) return;

    messageEl.textContent = message;

    toast.classList.remove(
        "toast--success",
        "toast--error",
        "toast--info",
        "toast--visible",
        "hidden"
    );

    if (type === "success") toast.classList.add("toast--success");
    else if (type === "error") toast.classList.add("toast--error");
    else toast.classList.add("toast--info");

    toast.classList.add("toast--visible");

    if (toastTimeoutId) clearTimeout(toastTimeoutId);
    toastTimeoutId = setTimeout(() => {
        toast.classList.remove("toast--visible");
    }, 2000);
}

async function fetchJson(url, options = {}) {
    const response = await fetch(url, {
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    if (!response.ok) {
        let message = `Ошибка ${response.status}`;
        try {
            const data = await response.json();
            if (data && data.message) message = data.message;
        } catch { /* ignore */ }
        throw new Error(message);
    }

    if (response.status === 204) return null;

    const text = await response.text();
    if (!text) return null;

    return JSON.parse(text);
}

async function fetchNoContent(url, options = {}) {
    const response = await fetch(url, {
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    if (!response.ok) {
        let message = `Ошибка ${response.status}`;
        try {
            const data = await response.json();
            if (data && data.message) message = data.message;
        } catch {}
        throw new Error(message);
    }
}

function getDishImageUrl(dish) {
    const valueRaw = dish.photo ?? dish.photoUrl ?? "";
    const value = String(valueRaw).trim();

    if (!value) return "img/food-placeholder.svg";

    if (value.startsWith("http://") || value.startsWith("https://")) return value;
    if (value.startsWith("/")) return value;

    return `/images/dishes/${value}`;
}

async function initMenu() {
    try {
        const [categories, dishes] = await Promise.all([
            fetchJson(`${AppConfig.apiBaseUrl}/menu/categories`),
            fetchJson(`${AppConfig.apiBaseUrl}/menu/dishes`)
        ]);

        state.categories = [{ id: 0, name: "Все" }, ...categories];
        state.dishes = dishes;

        renderCategories();
        renderDishes();
    } catch (error) {
        console.error("Ошибка загрузки меню:", error);
        showToast("Не удалось загрузить меню. Обратитесь к официанту.", "error");
    }
}

function renderCategories() {
    const container = document.getElementById("categories-list");
    if (container) {
        container.innerHTML = "";

        state.categories.forEach(category => {
            const btn = document.createElement("button");
            btn.className = "category-button";
            btn.textContent = category.name;
            btn.dataset.categoryId = category.id;

            if (category.id === state.activeCategoryId) {
                btn.classList.add("active");
            }

            btn.addEventListener("click", () => {
                state.activeCategoryId = category.id;
                renderCategories();
                renderDishes();
            });

            container.appendChild(btn);
        });
    }

    const select = document.getElementById("category-select");
    if (select) {
        select.innerHTML = "";
        state.categories.forEach(category => {
            const option = document.createElement("option");
            option.value = category.id;
            option.textContent = category.name;
            if (category.id === state.activeCategoryId) {
                option.selected = true;
            }
            select.appendChild(option);
        });
    }
}

function renderDishes() {
    const grid = document.getElementById("dishes-grid");
    if (!grid) return;
    grid.innerHTML = "";

    const dishesToShow =
        state.activeCategoryId === 0
            ? state.dishes
            : state.dishes.filter(d => d.categoryId === state.activeCategoryId);

    if (dishesToShow.length === 0) {
        const empty = document.createElement("div");
        empty.textContent = "В этой категории пока нет блюд.";
        empty.style.color = "#6b7280";
        empty.style.fontSize = "14px";
        grid.appendChild(empty);
        return;
    }

    dishesToShow.forEach(dish => {
        const card = document.createElement("article");
        card.className = "dish-card";
        card.dataset.dishId = dish.id;

        card.addEventListener("click", () => openDishModal(dish.id));

        const imgWrapper = document.createElement("div");
        imgWrapper.className = "dish-image-wrapper";
        const img = document.createElement("img");
        img.className = "dish-image";
        img.src = getDishImageUrl(dish);
        img.alt = dish.name;
        imgWrapper.appendChild(img);
        card.appendChild(imgWrapper);

        const content = document.createElement("div");
        content.className = "dish-content";

        const titleRow = document.createElement("div");
        titleRow.className = "dish-title-row";
        const title = document.createElement("div");
        title.className = "dish-title";
        title.textContent = dish.name;
        const price = document.createElement("div");
        price.className = "dish-price";
        price.textContent = `${dish.price} ₽`;
        titleRow.appendChild(title);
        titleRow.appendChild(price);
        content.appendChild(titleRow);

        const desc = document.createElement("div");
        desc.className = "dish-description";
        desc.textContent = dish.description || "";
        content.appendChild(desc);

        const actions = document.createElement("div");
        actions.className = "dish-actions";
        const addBtn = document.createElement("button");
        addBtn.className = "btn btn-primary";
        addBtn.textContent = "Добавить в заказ";
        addBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            addToCart(dish.id);
        });
        actions.appendChild(addBtn);
        content.appendChild(actions);

        card.appendChild(content);
        grid.appendChild(card);
    });
}

function addToCart(dishId) {
    const existing = state.cart.find(i => i.dishId === dishId);
    if (existing) existing.quantity += 1;
    else state.cart.push({ dishId, quantity: 1 });

    saveStateToStorage();

    const dish = state.dishes.find(d => d.id === dishId);
    const name = dish ? dish.name : "Блюдо";
    showToast(`«${name}» добавлено в заказ`, "success");
}

function openOrderModal() {
    const backdrop = document.getElementById("order-modal-backdrop");
    if (!backdrop) return;
    renderOrderModalContent();
    backdrop.classList.remove("hidden");
}

function closeOrderModal() {
    const backdrop = document.getElementById("order-modal-backdrop");
    if (!backdrop) return;
    backdrop.classList.add("hidden");
}

function renderOrderModalContent() {
    const body = document.getElementById("order-modal-body");
    const totalEl = document.getElementById("order-total-amount");
    if (!body || !totalEl) return;

    body.innerHTML = "";

    if (state.cart.length === 0) {
        const empty = document.createElement("div");
        empty.textContent = "Ваш заказ пуст.";
        empty.style.fontSize = "14px";
        empty.style.color = "#6b7280";
        body.appendChild(empty);
        totalEl.textContent = "0 ₽";
        return;
    }

    const list = document.createElement("div");
    list.className = "order-items-list";

    let total = 0;

    state.cart.forEach(item => {
        const dish = state.dishes.find(d => d.id === item.dishId);
        if (!dish) return;

        const row = document.createElement("div");
        row.className = "order-item-row";
        row.dataset.dishId = dish.id;

        const main = document.createElement("div");
        main.className = "order-item-main";

        const title = document.createElement("div");
        title.className = "order-item-title";
        title.textContent = dish.name;

        const meta = document.createElement("div");
        meta.className = "order-item-meta";
        meta.textContent = `${dish.price} ₽ / порция`;

        main.appendChild(title);
        main.appendChild(meta);

        const controls = document.createElement("div");
        controls.className = "order-item-controls";

        const minusBtn = document.createElement("button");
        minusBtn.className = "order-qty-btn";
        minusBtn.textContent = "−";
        minusBtn.dataset.action = "decrease";

        const qty = document.createElement("div");
        qty.className = "order-qty";
        qty.textContent = item.quantity;

        const plusBtn = document.createElement("button");
        plusBtn.className = "order-qty-btn";
        plusBtn.textContent = "+";
        plusBtn.dataset.action = "increase";

        const removeBtn = document.createElement("button");
        removeBtn.className = "order-remove-btn";
        removeBtn.textContent = "Удалить";
        removeBtn.dataset.action = "remove";

        const itemTotal = dish.price * item.quantity;
        total += itemTotal;

        const itemTotalEl = document.createElement("div");
        itemTotalEl.style.fontSize = "13px";
        itemTotalEl.style.fontWeight = "500";
        itemTotalEl.textContent = `${itemTotal} ₽`;

        controls.appendChild(minusBtn);
        controls.appendChild(qty);
        controls.appendChild(plusBtn);
        controls.appendChild(itemTotalEl);
        controls.appendChild(removeBtn);

        row.appendChild(main);
        row.appendChild(controls);

        list.appendChild(row);
    });

    body.appendChild(list);
    totalEl.textContent = `${total} ₽`;
}

function handleOrderItemClick(e) {
    const action = e.target.dataset.action;
    if (!action) return;

    const row = e.target.closest(".order-item-row");
    if (!row) return;

    const dishId = parseInt(row.dataset.dishId, 10);
    if (Number.isNaN(dishId)) return;

    const item = state.cart.find(i => i.dishId === dishId);
    if (!item) return;

    if (action === "increase") {
        item.quantity += 1;
    } else if (action === "decrease") {
        item.quantity = Math.max(1, item.quantity - 1);
    } else if (action === "remove") {
        state.cart = state.cart.filter(i => i.dishId !== dishId);
    }

    renderOrderModalContent();
    saveStateToStorage();
}

function openDishModal(dishId) {
    const dish = state.dishes.find(d => d.id === dishId);
    if (!dish) return;

    state.currentDishId = dishId;

    const backdrop = document.getElementById("dish-modal-backdrop");
    const titleEl = document.getElementById("dish-modal-title");
    const imgEl = document.getElementById("dish-modal-image");
    const priceEl = document.getElementById("dish-modal-price");
    const descEl = document.getElementById("dish-modal-description");
    const compEl = document.getElementById("dish-modal-composition");

    if (!backdrop || !titleEl || !imgEl || !priceEl || !descEl || !compEl) return;

    titleEl.textContent = dish.name;
    imgEl.src = getDishImageUrl(dish);
    imgEl.alt = dish.name;
    priceEl.textContent = `${dish.price} ₽`;
    descEl.textContent = dish.description || "Описание скоро появится.";

    compEl.textContent = "Загружаем состав...";
    compEl.style.display = "block";

    backdrop.classList.remove("hidden");

    loadDishComposition(dishId).then(composition => {
        if (state.currentDishId !== dishId) return;

        if (composition && composition.trim().length > 0) {
            compEl.textContent = "Состав: " + composition.trim();
            compEl.style.display = "block";
        } else {
            compEl.textContent = "Состав: уточните у официанта.";
            compEl.style.display = "block";
        }
    });
}

function closeDishModal() {
    const backdrop = document.getElementById("dish-modal-backdrop");
    if (!backdrop) return;
    backdrop.classList.add("hidden");
    state.currentDishId = null;
}

async function createCallWaiter(type) {
    if (!state.tableId) throw new Error("TableId is missing");

    const payload = {
        tableId: state.tableId,
        orderId: state.orderId && state.orderId > 0 ? state.orderId : null,
        type: type,
        isHandled: false
    };

    await fetchJson(`${AppConfig.apiBaseUrl}/callwaiter`, {
        method: "POST",
        body: JSON.stringify(payload)
    });
}

async function submitOrder() {
    if (!state.tableId) {
        showToast("Не удалось определить стол. Обратитесь к официанту.", "error");
        return;
    }

    if (state.cart.length === 0) {
        showToast("Ваш заказ пуст.", "info");
        return;
    }

    const now = Date.now();
    if (now - lastOrderSentAt < ORDER_COOLDOWN_MS) {
        showToast("Подождите немного перед повторной отправкой заказа.", "info");
        return;
    }

    try {
        let orderId = state.orderId;
        if (!orderId) {
            const createdOrder = await fetchJson(`${AppConfig.apiBaseUrl}/orders`, {
                method: "POST",
                body: JSON.stringify({
                    tableId: state.tableId,
                    status: "Preorder"
                })
            });
            orderId = createdOrder.id;
            state.orderId = orderId;
            state.orderStatus = createdOrder.status || "Preorder";
            updateOrderStatusUi();
        }

        const total = state.cart.reduce((sum, item) => {
            const dish = state.dishes.find(d => d.id === item.dishId);
            if (!dish) return sum;
            return sum + dish.price * item.quantity;
        }, 0);

        const items = state.cart.map(item => {
            const dish = state.dishes.find(d => d.id === item.dishId);
            if (!dish) throw new Error(`Блюдо с id=${item.dishId} не найдено`);

            return {
                dishId: dish.id,
                quantity: item.quantity,
                price: dish.price * item.quantity,
                notes: null,
                status: "Ordered"
            };
        });

        const dto = {
            orderId: orderId,
            tableId: state.tableId,
            orderStatus: "Preorder",
            guests: [
                {
                    index: 1,
                    items
                }
            ],
            payment: null,
            totalAmount: total
        };

        await fetchJson(`${AppConfig.apiBaseUrl}/orderdetails`, {
            method: "POST",
            body: JSON.stringify(dto)
        });

        lastOrderSentAt = now;
        saveStateToStorage();

        try {
            await createCallWaiter("AcceptPreorder");
            showToast("Заказ отправлен, официант скоро подойдёт.", "success");
        } catch (e) {
            console.error("Не удалось создать CallWaiter:", e);
            showToast("Заказ отправлен. Если официант не подходит, позовите его.", "error");
        }

        closeOrderModal();
    } catch (error) {
        console.error("Ошибка отправки предзаказа:", error);
        showToast("Не удалось отправить заказ. Попробуйте ещё раз.", "error");
    }
}

async function callWaiter() {
    if (!state.tableId) {
        showToast("Стол не указан. Обратитесь к официанту.", "error");
        return;
    }

    const now = Date.now();
    if (now - lastCallAt < CALL_COOLDOWN_MS) {
        showToast("Вы уже вызывали официанта. Немного подождите 🙂", "info");
        return;
    }

    const hasCart = state.cart.length > 0;
    const type = hasCart ? "AcceptPreorder" : "Call";

    try {
        await createCallWaiter(type);
        lastCallAt = now;

        if (type === "AcceptPreorder") {
            showToast("Предзаказ отправлен, официант скоро подойдёт.", "success");
        } else {
            showToast("Официант скоро подойдёт.", "success");
        }
    } catch (error) {
        console.error("Ошибка вызова официанта:", error);
        showToast("Не удалось вызвать официанта. Попробуйте ещё раз.", "error");
    }
}

function setupEventHandlers() {
    const viewOrderBtn = document.getElementById("view-order-btn");
    const callWaiterBtn = document.getElementById("call-waiter-btn");
    const orderModalCloseBtn = document.getElementById("order-modal-close");
    const orderModalCancelBtn = document.getElementById("order-modal-cancel");
    const orderModalSubmitBtn = document.getElementById("order-modal-submit");
    const orderBackdrop = document.getElementById("order-modal-backdrop");
    const orderBody = document.getElementById("order-modal-body");

    const dishModalCloseIcon = document.getElementById("dish-modal-close");
    const dishModalCloseBtn = document.getElementById("dish-modal-close-btn");
    const dishModalAddBtn = document.getElementById("dish-modal-add-btn");
    const dishBackdrop = document.getElementById("dish-modal-backdrop");

    const categorySelect = document.getElementById("category-select");

    if (viewOrderBtn) viewOrderBtn.addEventListener("click", openOrderModal);
    if (callWaiterBtn) callWaiterBtn.addEventListener("click", () => callWaiter());

    if (orderModalCloseBtn) orderModalCloseBtn.addEventListener("click", closeOrderModal);
    if (orderModalCancelBtn) orderModalCancelBtn.addEventListener("click", closeOrderModal);
    if (orderModalSubmitBtn) orderModalSubmitBtn.addEventListener("click", () => submitOrder());

    if (orderBackdrop) {
        orderBackdrop.addEventListener("click", (e) => {
            if (e.target === orderBackdrop) closeOrderModal();
        });
    }

    if (orderBody) orderBody.addEventListener("click", handleOrderItemClick);

    if (dishModalCloseIcon) dishModalCloseIcon.addEventListener("click", closeDishModal);
    if (dishModalCloseBtn) dishModalCloseBtn.addEventListener("click", closeDishModal);

    if (dishModalAddBtn) {
        dishModalAddBtn.addEventListener("click", () => {
            if (state.currentDishId != null) addToCart(state.currentDishId);
            closeDishModal();
        });
    }

    if (dishBackdrop) {
        dishBackdrop.addEventListener("click", (e) => {
            if (e.target === dishBackdrop) closeDishModal();
        });
    }

    if (categorySelect) {
        categorySelect.addEventListener("change", (e) => {
            const id = parseInt(e.target.value, 10);
            state.activeCategoryId = Number.isNaN(id) ? 0 : id;
            renderCategories();
            renderDishes();
        });
    }
}

async function loadDishComposition(dishId) {
    if (state.compositions[dishId]) {
        return state.compositions[dishId];
    }

    try {
        const items = await fetchJson(
            `${AppConfig.apiBaseUrl}/menu/dish-ingredients/${dishId}`
        );

        if (!items || !Array.isArray(items) || items.length === 0) {
            state.compositions[dishId] = "";
            return "";
        }

        const names = items
            .map(x => x.ingredientName)
            .filter(n => typeof n === "string" && n.trim().length > 0);

        const composition = names.join(", ");
        state.compositions[dishId] = composition;
        return composition;
    } catch (error) {
        console.error("Ошибка загрузки состава блюда:", error);
        state.compositions[dishId] = "";
        return "";
    }
}

let hubConnection = null;

function initRealtime() {
    if (!window.signalR) {
        console.warn("SignalR-клиент не найден. Real-time обновления отключены.");
        return;
    }

    const hubUrl = `${window.location.origin}/hubs/notifications`;

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    hubConnection.on("OrderChanged", onOrderChanged);
    hubConnection.on("CallWaiterChanged", onCallWaiterChanged);

    hubConnection.start()
        .then(() => console.log("SignalR подключен"))
        .catch(err => console.error("Ошибка подключения SignalR:", err));
}

function isActionUpdated(action) {
    return action === 1 || action === "Updated";
}

function onOrderChanged(notification) {
    if (!notification || !notification.order) return;
    const order = notification.order;

    if (!state.orderId || order.id !== state.orderId) return;

    const oldStatus = state.orderStatus;
    state.orderStatus = order.status;
    saveStateToStorage();
    updateOrderStatusUi();

    if (oldStatus !== order.status &&
        (order.status === "New" || order.status === "Pending")) {

        const waiterName = order.waiter && order.waiter.name
            ? order.waiter.name
            : null;

        if (waiterName) {
            showToast(`Официант ${waiterName} принял ваш заказ и уже в пути.`, "info");
        } else {
            showToast("Официант принял ваш заказ и уже в пути.", "info");
        }
    }

    if (order.status === "Closed" && !state.hasRated) {
        openRatingModal();
    }
}

function onCallWaiterChanged(notification) {
    if (!notification || !notification.callWaiter) return;
    const cw = notification.callWaiter;

    if (!state.tableId || cw.tableId !== state.tableId) return;

    if (isActionUpdated(notification.action) && cw.isHandled) {
        showToast("Официант принял ваш вызов и уже в пути.", "info");
    }
}

function initRatingControls() {
    const backdrop = document.getElementById("rating-modal-backdrop");
    const closeIcon = document.getElementById("rating-modal-close");
    const skipBtn = document.getElementById("rating-skip-btn");
    const submitBtn = document.getElementById("rating-submit-btn");
    const starsContainer = document.getElementById("rating-stars");

    if (!backdrop || !starsContainer) return;

    starsContainer.innerHTML = "";
    for (let i = 1; i <= 5; i++) {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "rating-star-btn";
        btn.dataset.value = String(i);
        btn.innerText = "★";
        btn.addEventListener("click", () => {
            state.ratingScore = i;
            updateRatingStarsUi();
        });
        starsContainer.appendChild(btn);
    }

    if (closeIcon) closeIcon.addEventListener("click", () => {
        state.ratingScore = null;
        closeRatingModal();
    });

    if (skipBtn) skipBtn.addEventListener("click", () => {
        state.hasRated = true;
        saveStateToStorage();
        closeRatingModal();
    });

    if (submitBtn) submitBtn.addEventListener("click", async () => {
        const commentEl = document.getElementById("rating-comment");
        const comment = commentEl ? commentEl.value.trim() : "";

        if (!state.ratingScore) {
            showToast("Пожалуйста, выберите оценку.", "info");
            return;
        }

        try {
            state.hasRated = true;
            saveStateToStorage();
            showToast("Спасибо за ваш отзыв!", "success");
            closeRatingModal();
        } catch (e) {
            console.error("Ошибка отправки отзыва:", e);
            showToast("Не удалось отправить отзыв. Попробуйте позже.", "error");
        }
    });

    backdrop.addEventListener("click", (e) => {
        if (e.target === backdrop) closeRatingModal();
    });
}

function updateRatingStarsUi() {
    const starsContainer = document.getElementById("rating-stars");
    if (!starsContainer) return;
    const buttons = starsContainer.querySelectorAll(".rating-star-btn");
    buttons.forEach(btn => {
        const value = parseInt(btn.dataset.value, 10);
        if (state.ratingScore && value <= state.ratingScore) {
            btn.classList.add("rating-star-btn--active");
        } else {
            btn.classList.remove("rating-star-btn--active");
        }
    });
}

function openRatingModal() {
    const backdrop = document.getElementById("rating-modal-backdrop");
    if (!backdrop) return;
    if (state.hasRated) return;

    backdrop.classList.remove("hidden");
    state.ratingScore = null;
    updateRatingStarsUi();

    const commentEl = document.getElementById("rating-comment");
    if (commentEl) commentEl.value = "";
}

function closeRatingModal() {
    const backdrop = document.getElementById("rating-modal-backdrop");
    if (!backdrop) return;
    backdrop.classList.add("hidden");
}



