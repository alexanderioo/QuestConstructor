const state = {
    quests: [],
    currentQuest: null,
    selectedSceneId: null,
    isNew: false,
    game: null
};

const elements = {
    questList: document.querySelector("#quest-list"),
    questCount: document.querySelector("#quest-count"),
    pageTitle: document.querySelector("#page-title"),
    editor: document.querySelector("#editor"),
    emptyState: document.querySelector("#empty-state"),
    playground: document.querySelector("#playground"),
    notice: document.querySelector("#notice"),
    sceneList: document.querySelector("#scene-list"),
    sceneEditor: document.querySelector("#scene-editor"),
    validationDialog: document.querySelector("#validation-dialog")
};

document.querySelector("#new-quest-button").addEventListener("click", createNewQuest);
document.querySelector("#empty-new-button").addEventListener("click", createNewQuest);
document.querySelector("#add-scene-button").addEventListener("click", addScene);
document.querySelector("#save-button").addEventListener("click", () => saveQuest(true));
document.querySelector("#validate-button").addEventListener("click", validateQuest);
document.querySelector("#play-button").addEventListener("click", startGame);
document.querySelector("#back-to-editor-button").addEventListener("click", showEditor);
document.querySelector("#restart-game-button").addEventListener("click", startGame);
document.querySelector("#close-validation-button").addEventListener("click", () => elements.validationDialog.close());

document.querySelector("#quest-title").addEventListener("input", event => {
    if (!state.currentQuest) return;
    state.currentQuest.title = event.target.value;
    elements.pageTitle.textContent = event.target.value || "Без названия";
});

document.querySelector("#quest-description").addEventListener("input", event => {
    if (state.currentQuest) state.currentQuest.description = event.target.value;
});

elements.questList.addEventListener("click", event => {
    const button = event.target.closest("[data-quest-id]");
    if (button) selectQuest(button.dataset.questId);
});

elements.sceneList.addEventListener("click", event => {
    const button = event.target.closest("[data-scene-id]");
    if (!button) return;
    state.selectedSceneId = button.dataset.sceneId;
    renderSceneList();
    renderSceneEditor();
});

elements.sceneEditor.addEventListener("input", handleSceneInput);
elements.sceneEditor.addEventListener("change", handleSceneInput);
elements.sceneEditor.addEventListener("click", handleSceneAction);

initialize();

async function initialize() {
    try {
        await refreshQuestList();
        if (state.quests.length > 0) {
            await selectQuest(state.quests[0].id);
        } else {
            renderEmptyState();
        }
    } catch (error) {
        showNotice(error.message, true);
        renderEmptyState();
    }
}

async function api(url, options = {}) {
    const response = await fetch(url, {
        headers: { "Content-Type": "application/json", ...(options.headers || {}) },
        ...options
    });

    if (response.status === 204) return null;

    const contentType = response.headers.get("content-type") || "";
    const body = contentType.includes("application/json") ? await response.json() : null;
    if (!response.ok) {
        throw new Error(body?.message || `Ошибка сервера: ${response.status}`);
    }

    return body;
}

async function refreshQuestList() {
    state.quests = await api("/api/quests");
    renderQuestList();
}

async function selectQuest(id) {
    state.currentQuest = await api(`/api/quests/${id}`);
    state.selectedSceneId = state.currentQuest.startSceneId || state.currentQuest.scenes[0]?.id || null;
    state.isNew = false;
    showEditor();
    renderAll();
}

function createNewQuest() {
    const firstSceneId = crypto.randomUUID();
    state.currentQuest = {
        id: crypto.randomUUID(),
        title: "Новый квест",
        description: "",
        startSceneId: firstSceneId,
        scenes: [{
            id: firstSceneId,
            title: "Начало",
            text: "Опишите, с чего начинается история.",
            isEnding: false,
            endingText: "",
            choices: []
        }],
        playCount: 0,
        completionCount: 0
    };
    state.selectedSceneId = firstSceneId;
    state.isNew = true;
    showEditor();
    renderAll();
    document.querySelector("#quest-title").select();
}

function renderAll() {
    const quest = state.currentQuest;
    if (!quest) return;

    elements.pageTitle.textContent = quest.title || "Без названия";
    document.querySelector("#quest-title").value = quest.title;
    document.querySelector("#quest-description").value = quest.description;
    document.querySelector("#scene-stat").textContent = quest.scenes.length;
    document.querySelector("#ending-stat").textContent = quest.scenes.filter(scene => scene.isEnding).length;
    document.querySelector("#play-stat").textContent = quest.playCount || 0;
    document.querySelector("#completion-stat").textContent = quest.completionCount || 0;
    renderQuestList();
    renderSceneList();
    renderSceneEditor();
}

function renderQuestList() {
    elements.questCount.textContent = state.quests.length;
    elements.questList.innerHTML = state.quests.map(quest => `
        <button class="quest-list-item ${quest.id === state.currentQuest?.id ? "active" : ""}"
                data-quest-id="${quest.id}" type="button">
            <span class="quest-dot"></span>
            <span>
                <strong>${escapeHtml(quest.title || "Без названия")}</strong>
                <small>${quest.scenes.length} сцен · ${quest.playCount || 0} запусков</small>
            </span>
        </button>
    `).join("");
}

function renderSceneList() {
    const quest = state.currentQuest;
    if (!quest) return;

    elements.sceneList.innerHTML = quest.scenes.map((scene, index) => `
        <button class="scene-item ${scene.id === state.selectedSceneId ? "active" : ""}"
                data-scene-id="${scene.id}" type="button">
            <span class="scene-index">${String(index + 1).padStart(2, "0")}</span>
            <strong>${escapeHtml(scene.title || "Без названия")}</strong>
            <small class="${scene.isEnding ? "scene-badge" : ""}">
                ${scene.id === quest.startSceneId ? "Стартовая · " : ""}
                ${scene.isEnding ? "Концовка" : `${scene.choices.length} переходов`}
            </small>
        </button>
    `).join("");
}

function renderSceneEditor() {
    const quest = state.currentQuest;
    const scene = currentScene();
    if (!quest || !scene) {
        elements.sceneEditor.innerHTML = `
            <div class="scene-editor-empty">
                <h2>Выберите сцену</h2>
                <p>Настройки выбранной сцены появятся здесь.</p>
            </div>`;
        return;
    }

    const targetOptions = quest.scenes.map(item =>
        `<option value="${item.id}">${escapeHtml(item.title || "Без названия")}</option>`
    ).join("");

    const choices = scene.choices.map((choice, index) => `
        <div class="choice-row" data-choice-id="${choice.id}">
            <input data-choice-field="text" value="${escapeAttribute(choice.text)}"
                   placeholder="Текст выбора игрока" aria-label="Текст выбора">
            <select data-choice-field="nextSceneId" aria-label="Следующая сцена">
                <option value="">Выберите следующую сцену</option>
                ${targetOptions.replace(`value="${choice.nextSceneId}"`, `value="${choice.nextSceneId}" selected`)}
            </select>
            <button class="icon-button remove-choice" data-action="remove-choice"
                    data-choice-id="${choice.id}" type="button" title="Удалить переход">×</button>
            <div class="choice-details">
                <label>Нужный предмет
                    <input data-choice-field="requiredItem" value="${escapeAttribute(choice.requiredItem)}"
                           placeholder="Например: Ключ">
                </label>
                <label>Получаемый предмет
                    <input data-choice-field="grantedItem" value="${escapeAttribute(choice.grantedItem)}"
                           placeholder="Например: Фонарь">
                </label>
                <label>Изменение здоровья
                    <input data-choice-field="healthChange" type="number" min="-100" max="100"
                           value="${choice.healthChange || 0}">
                </label>
                <label class="toggle-card">
                    <input data-choice-field="consumesRequiredItem" type="checkbox"
                           ${choice.consumesRequiredItem ? "checked" : ""}>
                    Потратить предмет
                </label>
            </div>
        </div>
    `).join("");

    elements.sceneEditor.innerHTML = `
        <div class="section-heading">
            <div><span class="section-number">02</span><h2>Редактор сцены</h2></div>
            <span class="section-number">${scene.id === quest.startSceneId ? "START" : scene.isEnding ? "ENDING" : "SCENE"}</span>
        </div>

        <div class="scene-form-grid">
            <div class="field">
                <label for="scene-title">Название сцены</label>
                <input id="scene-title" data-scene-field="title" maxlength="100"
                       value="${escapeAttribute(scene.title)}" placeholder="Название">
            </div>
            <label class="toggle-card">
                <input data-scene-field="isEnding" type="checkbox" ${scene.isEnding ? "checked" : ""}>
                Это финальная сцена
            </label>
        </div>

        <div class="field scene-text-field">
            <label for="scene-text">Текст сцены</label>
            <textarea id="scene-text" data-scene-field="text" rows="5"
                      placeholder="Что видит игрок?">${escapeHtml(scene.text)}</textarea>
        </div>

        <div class="ending-fields ${scene.isEnding ? "" : "hidden"}">
            <div class="field">
                <label for="ending-text">Текст концовки</label>
                <textarea id="ending-text" data-scene-field="endingText" rows="3"
                          placeholder="Подведите итог истории">${escapeHtml(scene.endingText)}</textarea>
            </div>
        </div>

        <div class="choices-section ${scene.isEnding ? "hidden" : ""}">
            <div class="section-heading">
                <div><span class="section-number">03</span><h2>Варианты выбора</h2></div>
                <button class="button button-ghost" data-action="add-choice" type="button">＋ Добавить</button>
            </div>
            <div id="choice-list">
                ${choices || `<div class="choice-empty">У этой сцены пока нет переходов.</div>`}
            </div>
        </div>

        <div class="scene-actions">
            <button class="button button-ghost" data-action="set-start" type="button"
                    ${scene.id === quest.startSceneId ? "disabled" : ""}>Сделать стартовой</button>
            <button class="button button-danger" data-action="delete-scene" type="button"
                    ${quest.scenes.length === 1 ? "disabled" : ""}>Удалить сцену</button>
        </div>`;
}

function handleSceneInput(event) {
    const scene = currentScene();
    if (!scene) return;

    const sceneField = event.target.dataset.sceneField;
    if (sceneField) {
        scene[sceneField] = event.target.type === "checkbox" ? event.target.checked : event.target.value;
        if (sceneField === "title") {
            renderSceneList();
        }

        if (sceneField === "isEnding") {
            if (scene.isEnding) scene.choices = [];
            renderSceneList();
            renderSceneEditor();
        }
        updateStats();
        return;
    }

    const choiceField = event.target.dataset.choiceField;
    const choiceContainer = event.target.closest("[data-choice-id]");
    if (!choiceField || !choiceContainer) return;

    const choice = scene.choices.find(item => item.id === choiceContainer.dataset.choiceId);
    if (!choice) return;

    if (event.target.type === "checkbox") {
        choice[choiceField] = event.target.checked;
    } else if (event.target.type === "number") {
        choice[choiceField] = Number(event.target.value);
    } else {
        choice[choiceField] = event.target.value;
    }
}

function handleSceneAction(event) {
    const actionButton = event.target.closest("[data-action]");
    if (!actionButton) return;

    const scene = currentScene();
    if (!scene) return;

    switch (actionButton.dataset.action) {
        case "add-choice":
            scene.choices.push({
                id: crypto.randomUUID(),
                text: "",
                nextSceneId: "",
                requiredItem: "",
                grantedItem: "",
                consumesRequiredItem: false,
                healthChange: 0
            });
            renderSceneList();
            renderSceneEditor();
            break;
        case "remove-choice":
            scene.choices = scene.choices.filter(choice => choice.id !== actionButton.dataset.choiceId);
            renderSceneList();
            renderSceneEditor();
            break;
        case "set-start":
            state.currentQuest.startSceneId = scene.id;
            renderSceneList();
            renderSceneEditor();
            break;
        case "delete-scene":
            deleteCurrentScene();
            break;
    }
}

function addScene() {
    if (!state.currentQuest) return;

    const scene = {
        id: crypto.randomUUID(),
        title: `Сцена ${state.currentQuest.scenes.length + 1}`,
        text: "",
        isEnding: false,
        endingText: "",
        choices: []
    };
    state.currentQuest.scenes.push(scene);
    state.selectedSceneId = scene.id;
    renderAll();
}

function deleteCurrentScene() {
    const quest = state.currentQuest;
    const scene = currentScene();
    if (!quest || !scene || quest.scenes.length === 1) return;
    if (!confirm(`Удалить сцену «${scene.title}» и ведущие к ней переходы?`)) return;

    quest.scenes = quest.scenes.filter(item => item.id !== scene.id);
    for (const item of quest.scenes) {
        item.choices = item.choices.filter(choice => choice.nextSceneId !== scene.id);
    }
    if (quest.startSceneId === scene.id) quest.startSceneId = quest.scenes[0].id;
    state.selectedSceneId = quest.scenes[0].id;
    renderAll();
}

async function saveQuest(showMessage) {
    const quest = state.currentQuest;
    if (!quest) return null;

    const url = state.isNew ? "/api/quests" : `/api/quests/${quest.id}`;
    const method = state.isNew ? "POST" : "PUT";
    const saved = await api(url, { method, body: JSON.stringify(quest) });

    state.currentQuest = saved;
    state.isNew = false;
    await refreshQuestList();
    if (showMessage) showNotice("Квест сохранён.");
    renderAll();
    return saved;
}

async function validateQuest() {
    try {
        const saved = await saveQuest(false);
        if (!saved) return;

        const result = await api(`/api/quests/${saved.id}/validate`, { method: "POST" });
        const errorCount = result.issues.filter(issue => issue.level === "error").length;
        const warningCount = result.issues.filter(issue => issue.level === "warning").length;

        document.querySelector("#validation-title").textContent =
            result.canStart ? "Квест готов к запуску" : "Найдены ошибки";
        document.querySelector("#validation-summary").textContent =
            `${result.reachableSceneCount} из ${result.sceneCount} сцен достижимы · ` +
            `${errorCount} ошибок · ${warningCount} предупреждений`;

        document.querySelector("#validation-list").innerHTML = result.issues.length
            ? result.issues.map(issue => `
                <div class="validation-item ${issue.level}">
                    <span class="validation-level">${issue.level === "error" ? "Ошибка" : "Совет"}</span>
                    <span>${escapeHtml(issue.message)}</span>
                </div>`).join("")
            : `<div class="validation-success">Все переходы работают. Квест можно проходить.</div>`;

        elements.validationDialog.showModal();
    } catch (error) {
        showNotice(error.message, true);
    }
}

async function startGame() {
    try {
        const saved = await saveQuest(false);
        if (!saved) return;

        state.game = await api("/api/games/start", {
            method: "POST",
            body: JSON.stringify({ questId: saved.id })
        });
        renderGame();
        elements.editor.classList.add("hidden");
        elements.emptyState.classList.add("hidden");
        elements.playground.classList.remove("hidden");
        document.querySelector("#save-button").disabled = true;
        document.querySelector("#validate-button").disabled = true;
        document.querySelector("#play-button").disabled = true;
    } catch (error) {
        showNotice(error.message, true);
    }
}

function renderGame() {
    const game = state.game;
    if (!game) return;

    document.querySelector("#game-quest-title").textContent = game.questTitle;
    document.querySelector("#game-health").textContent = game.health;
    document.querySelector("#game-scene-title").textContent = game.sceneTitle;
    document.querySelector("#game-scene-text").textContent = game.sceneText;

    document.querySelector("#game-choices").innerHTML = game.choices.map(choice => `
        <button class="game-choice" data-game-choice-id="${choice.id}" type="button"
                ${choice.canChoose ? "" : "disabled"}>
            <span>${escapeHtml(choice.text)}</span>
            ${choice.unavailableReason ? `<small>${escapeHtml(choice.unavailableReason)}</small>` : "<span>→</span>"}
        </button>
    `).join("");

    const inventory = document.querySelector("#inventory-list");
    inventory.innerHTML = game.inventory.map(item =>
        `<div class="inventory-item">${escapeHtml(item)}</div>`
    ).join("");
    document.querySelector("#inventory-empty").classList.toggle("hidden", game.inventory.length > 0);

    document.querySelector("#game-ending").classList.toggle("hidden", !game.isCompleted);
    document.querySelector("#game-ending-text").textContent = game.endingText;

    document.querySelectorAll("[data-game-choice-id]").forEach(button => {
        button.addEventListener("click", () => makeGameChoice(button.dataset.gameChoiceId));
    });
}

async function makeGameChoice(choiceId) {
    try {
        state.game = await api(`/api/games/${state.game.sessionId}/choices/${choiceId}`, {
            method: "POST"
        });
        renderGame();
    } catch (error) {
        showNotice(error.message, true);
    }
}

function showEditor() {
    elements.editor.classList.toggle("hidden", !state.currentQuest);
    elements.emptyState.classList.toggle("hidden", Boolean(state.currentQuest));
    elements.playground.classList.add("hidden");
    document.querySelector("#save-button").disabled = !state.currentQuest;
    document.querySelector("#validate-button").disabled = !state.currentQuest;
    document.querySelector("#play-button").disabled = !state.currentQuest;
}

function renderEmptyState() {
    state.currentQuest = null;
    elements.pageTitle.textContent = "Конструктор квестов";
    showEditor();
}

function currentScene() {
    return state.currentQuest?.scenes.find(scene => scene.id === state.selectedSceneId) || null;
}

function updateStats() {
    const quest = state.currentQuest;
    if (!quest) return;
    document.querySelector("#scene-stat").textContent = quest.scenes.length;
    document.querySelector("#ending-stat").textContent = quest.scenes.filter(scene => scene.isEnding).length;
}

function showNotice(message, isError = false) {
    elements.notice.textContent = message;
    elements.notice.classList.remove("hidden");
    elements.notice.classList.toggle("error", isError);
    clearTimeout(showNotice.timer);
    showNotice.timer = setTimeout(() => elements.notice.classList.add("hidden"), 4200);
}

function escapeHtml(value = "") {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function escapeAttribute(value = "") {
    return escapeHtml(value).replaceAll("\n", "&#10;");
}
