# DEV PROGRESS — Научное Золотое кольцо (Unity)

> ⚠️ ДЛЯ НОВОГО ГОРОДА ЧИТАЙ СНАЧАЛА **CITY_RECIPE.md** (в корне проекта) — там актуальная архитектура города и пошаговый рецепт (образец — Кострома). Блоки ниже — история; при расхождении верен CITY_RECIPE.md и самые свежие датированные блоки.

Заметка для продолжения в новом чате. Обновлено: 27.09.2026.
Читать вместе с PROJECT_BRIEF.md (общий контекст).

## Как продолжить в новом чате
1. Проект GoldenRing открыт в Unity, MCP подключён (Window → MCP for Unity → reconnect при обрыве).
2. Проверить связь: активная сцена + консоль (любая read-команда MCP).
3. Прочитать этот файл (лежит в корне проекта) и PROJECT_BRIEF.md.
4. Продолжить со СЛЕДУЮЩЕЙ ЗАДАЧИ (ниже).

## Сделано (ядро прогресса, проверено тестом логики)
- Assets/_Project/Scripts/Core/CityData.cs — ScriptableObject города (id, displayName, order, citySceneName, quizSceneName, artifactId, artifactDisplayName, artifactPrefab).
- Assets/_Project/Scripts/Core/GameConfig.cs — список городов по порядку (GetByOrder, GetById, First, IndexOf).
- Assets/_Project/Scripts/Core/GameManager.cs — синглтон, DontDestroyOnLoad. API:
  IsCityUnlocked(city), CollectArtifact(city), PlaceArtifact(city), IsArtifactCollected/Placed(city),
  CompleteQuiz(city) [открывает следующий город], AreAllArtifactsPlaced(), ResetProgress(), InitProgress().
  События: OnArtifactCollected, OnArtifactPlaced, OnQuizCompleted, OnCityUnlocked, OnAllArtifactsPlaced, OnProgressReset.
- Данные: Assets/_Project/Data/Cities/City_*.asset (9 городов) + Assets/_Project/Data/GameConfig.asset (назначен в GameManager).
- Сцена Assets/_Project/Scenes/Core/Bootstrap.unity — объект GameManager с назначенным GameConfig; Bootstrap первая в Build Settings (индекс 0). GameManager живёт всегда, хранит прогресс и артефакты между сценами.
- Порядок городов: Ярославль(0), Кострома(1), Переславль(2), Иваново(3), Владимир(4), Суздаль(5), Ростов(6), Сергиев Посад(7), Москва(8). Меняется в CityData.order.
- Имена сцен городов/викторин — плейсхолдеры (City_*, Quiz_*), реальных сцен ещё нет.

## Сделано (взаимодействие с артефактом — вертикальный срез, 27.09.2026)
Скрипты-мосты (Assembly-CSharp, без asmdef; XRI 3.5.1):
- Assets/_Project/Scripts/Interaction/ArtifactPickup.cs — на префабе артефакта рядом с XRGrabInteractable.
  Хранит ссылку на CityData. По selectEntered рукой (сокет игнорируется) → GameManager.CollectArtifact(city).
  GameManager может отсутствовать (песочница) — тогда просто лог, без ошибок.
- Assets/_Project/Scripts/Interaction/StandSocket.cs — на узле Socket рядом с XRSocketInteractor.
  Реализует IXRSelectFilter: в гнездо пускает ТОЛЬКО артефакт своего города (сверка по artifactId).
  По selectEntered → GameManager.PlaceArtifact(city) (финал GameManager ловит сам через OnAllArtifactsPlaced).
Сцена-песочница Assets/_Project/Scenes/_Sandbox/Eugen_ArtifactTest.unity:
- XR Origin (XR Rig) из Starter Assets + отдельный XR Interaction Manager + XR Interaction Simulator (тест без шлема).
- Floor (Plane), Directional Light, Table (опора под артефакт).
- Артефакт: префаб Assets/_Project/Prefabs/Artifacts/Artifact_Yaroslavl.prefab (куб 15 см + Rigidbody + BoxCollider + XRGrabInteractable + ArtifactPickup → City_yaroslavl).
- Стенд: префаб Assets/_Project/Prefabs/Stands/Stand_Yaroslavl.prefab (корень + Podium-цилиндр + узел Socket с SphereCollider-триггером r=0.15 + XRSocketInteractor + StandSocket → City_yaroslavl).
- Временный GameManager (Sandbox) с назначенным GameConfig — чтобы срез работал автономно.
Проверено: компиляция чистая; вход в Play — без ошибок/NullRef (одно безобидное предупреждение симулятора, см. ниже).

## НАДО ДОТЕСТИТЬ вручную в XR Device Simulator (Eugen)
Запустить Eugen_ArtifactTest, взять куб левой рукой, поставить на стенд. Ожидаемо в консоли:
CollectArtifact при взятии рукой и PlaceArtifact при установке в гнездо (стенд принимает только свой артефакт).

## СЛЕДУЮЩАЯ ЗАДАЧА: доводка взаимодействия
1. Хват ТОЛЬКО левой рукой. Сейчас грэб разрешён обеими руками (interaction layers по умолчанию).
   Сделать слой Interaction Layer «Artifact», добавить его артефакту и ЛЕВОМУ интерактору, убрать у правого
   (правая рука — телепорт/перемещение). Проверить в симуляторе.
2. Переимпортировать сэмплы XRI под 3.5.1 (сейчас в Assets/Samples лежат 3.2.2). Из-за расхождения версий
   симулятор пишет предупреждение про SimulatedHandPlaybackManager (hand playback). Package Manager →
   XR Interaction Toolkit 3.5.1 → Samples → Reimport Starter Assets + XR Interaction Simulator.
3. Привязать «взятый артефакт к руке» визуально/логически по кейсу (клеится к левой руке до постановки на стенд).
4. Обобщить: сделать артефакт и стенд шаблонными, поле CityData назначать на каждый из 9 городов
   (артефакты пока кубы-плейсхолдеры — заменить моделями Никиты позже).

## Дальше по плану
- Переходы сцен с fade + лобби/менеджеры в постоянной сцене (additive).
- Лобби с картой (клик по городу, замок на закрытых).
- Система викторин (data-driven, Таня).
- Робот-гид (NavMesh follow + ТВ-UI).
- Финал (карта замыкается по OnAllArtifactsPlaced → дверь → статистика).
- Клонирование шаблонной сцены по 9 городам.
- ГЕЙТ: собрать пустой .apk на Quest как можно раньше.

## Заметки
- Артефакт НЕ хранить как объект сцены между сценами — факт сбора уже в GameManager; в лобби предмет пересоздаётся по данным.
- Управление: левая рука — предметы, правая — ходьба; часть викторин — обе руки.
- _Sandbox-сцены в билд не идут; боевые сцены — в Core/Cities/Quizzes.

## Обновление 27.09.2026 (доводка среза + ручной тест прошёл)
- Хват левой рукой и постановка на стенд ПРОВЕРЕНЫ в XR Device Simulator — работают.
- Убраны 2 пустых дубля Stand_Yaroslavl (побочка create_from_gameobject) — остался 1 рабочий стенд.
- СДЕЛАНО «хват только левой рукой»: правый Near-Far Interactor.interactionLayers = Nothing (грэб выключен),
  левый = Default. Телепорт правой руки — отдельный Teleport Interactor (XRRayInteractor), НЕ затронут; поук обеих рук цел.
- Добавлен тест-логгер Assets/_Project/Scripts/Interaction/SandboxEventLogger.cs на объект GameManager (Sandbox):
  пишет в консоль [GR] Collect/Place/финал/открытие города. В боевые сцены НЕ тащить (это тест-хелпер).
  Раньше логов не было не из-за ошибки — GameManager в сцене есть, мосты пишут в консоль только когда его НЕТ.
- Косметические варнинги симулятора (SimulatedHandPlaybackManager, haptic capabilities error -1) — от расхождения
  сэмплов XRI 3.2.2 с пакетом 3.5.1. На хват/сокет/билд не влияют. Лечится переимпортом сэмплов под 3.5.1.

## Осталось из доводки
- Переимпортировать сэмплы XRI под 3.5.1 (Package Manager → XR Interaction Toolkit 3.5.1 → Samples →
  Reimport: Starter Assets + XR Interaction Simulator). Только чтобы убрать варнинги — не срочно.
- Обобщить артефакт/стенд на 9 городов (сейчас только Ярославль; кубы-плейсхолдеры → модели Никиты позже).
- Клеить взятый артефакт к руке визуально до постановки (по кейсу).

## Обновление 27.09.2026 (пункт 1 — сброс стендов)
- StandSocket теперь подписан на GameManager.OnProgressReset и выбрасывает артефакт из гнезда при сбросе.
  Подписка отложенная (OnEnable + Start) — переживает случай, когда GameManager (Bootstrap) появляется позже сокета.
- Проверено автотестом в Play: установка → selected=1, IsPlaced=True, PlacedCount=1; после ResetProgress → selected=0,
  IsPlaced=False, PlacedCount=0. Логи [GR] Поставлен/Сброшен идут. Файл перезаписан на месте (guid .meta сохранён).
- Задача «взаимодействие с артефактом» закрыта полностью. Следующий крупный этап: переходы сцен (fade) + лобби
  (обсуждается детально отдельно).

## Тест-кнопка перезапуска (песочница, 27.09.2026)
- Assets/_Project/Scripts/Interaction/RestartButton.cs — на uGUI Button; по клику GameManager.ResetProgress()
  + возврат всех ArtifactPickup на старт (ArtifactPickup.ResetToStart(): снять выбор, обнулить скорость, вернуть позу).
- В Eugen_ArtifactTest добавлены: EventSystem (XRUIInputModule), world-space Canvas "Restart Canvas"
  (renderMode WorldSpace, GraphicRaycaster + TrackedDeviceGraphicRaycaster, scale 0.0025) с кнопкой "RESTART".
  Позиция кнопки [0,1.5,1], смотрит на игрока. Кликается лучом (UI-взаимодействие не зависит от grab-слоёв).
- Подпись латиницей ("RESTART"): дефолтный шрифт TMP без кириллицы; для боевого UI поставить шрифт с кириллицей.
- Проверено в Play: place->PlacedCount=1; после клика Restart -> PlacedCount=0, сокет пуст, артефакт вернулся на старт.
- Всё это тест-хелперы песочницы, в боевые сцены не тащить.

## Фикс кнопки Restart (27.09.2026)
- Баг: при Restart артефакт мелькал, телепортировался назад и оставался на стенде; повторная установка не логировалась.
  Причина одна: сокет в том же кадре перехватывал артефакт обратно (физтриггеры не успевали обновиться), он оставался выбранным.
- Фикс: ArtifactPickup.ResetToStart() теперь корутина — на 1 кадр выключает XRGrabInteractable (снимает выбор и hovered),
  перемещает артефакт на старт, ждёт WaitForFixedUpdate + кадр, включает обратно. Сокет больше не перехватывает.
- Проверено в Play: после Restart socketSel=0, артефакт на столике; повторная установка -> placed=1 и лог 'Поставлен' идёт.

## Роль Restart в финале (зафиксировано)
- Restart = полный перезапуск игры: с 1-го города, викторины заново, артефакты на местах. GameManager.ResetProgress()
  это моделирует. В бою кнопка (финал/пауза) = ResetProgress() + переход на стартовую сцену; артефакты пересоздаются
  по данным при заходе в сцену, поэтому 'возвращаются на места' сами.

## Разбор 'логи не пишутся повторно' (27.09.2026)
- Дверь ПРОВЕРЕНА: тест 'поставить все 9 -> сброс' x3 -> OnAllArtifactsPlaced срабатывает каждый раз (allFired=3),
  placed каждый цикл 0->9. Логика GameManager идемпотентна, дверь откроется на любом прохождении.
- 'Логи не пишутся повторно' = визуальный эффект кнопки Collapse в консоли Unity: одинаковый текст схлопывался в 1 строку.
  Событие OnArtifactPlaced срабатывало каждый раз (подтверждено: 2 записи через MCP).
- SandboxEventLogger: сообщения теперь со сквозным счётчиком (#N) — каждая постановка отдельной строкой даже при Collapse.
  Проверено в Play: цикл place#1 -> Restart -> place#2 даёт в консоли #1 и #2 отдельно.


=== АКТУАЛЬНО НА 27.09.2026 (НАЧИНАТЬ НОВЫЙ ЧАТ ОТСЮДА) ===

## Что готово по взаимодействию с артефактом (вертикальный срез ЗАКРЫТ)
Скрипты (Assets/_Project/Scripts/Interaction/, namespace GoldenRing.Interaction, Assembly-CSharp, XRI 3.5.1):
- ArtifactPickup.cs — на артефакте (рядом с XRGrabInteractable). Взятие рукой -> GameManager.CollectArtifact.
  Метод ResetToStart() (корутина: на 1 кадр выключает XRGrabInteractable, чтобы сокет не перехватил, возвращает на старт).
- StandSocket.cs — на узле Socket (рядом с XRSocketInteractor). IXRSelectFilter: пускает только свой артефакт по artifactId.
  Постановка -> GameManager.PlaceArtifact. По OnProgressReset выбрасывает артефакт из гнезда.
- SandboxEventLogger.cs — ТЕСТ-ХЕЛПЕР на GameManager (Sandbox). Логи [GR] со счётчиком #N.
- RestartButton.cs — ТЕСТ-ХЕЛПЕР на uGUI Button. Клик = ResetProgress() + ResetToStart() всех артефактов.
Сцена Assets/_Project/Scenes/_Sandbox/Eugen_ArtifactTest.unity: XR Origin + XR Interaction Manager + XR Interaction Simulator,
  Floor/Light/Table, GameManager (Sandbox)+GameConfig, EventSystem(XRUIInputModule), world-space Canvas с кнопкой RESTART.
Префабы: Prefabs/Artifacts/Artifact_Yaroslavl.prefab, Prefabs/Stands/Stand_Yaroslavl.prefab (оба -> City_yaroslavl).
Проверено вручную (Eugen) и автотестами: хват ТОЛЬКО левой рукой, постановка, фильтр свой/чужой, сброс, дверь (OnAllArtifactsPlaced x3).
ВАЖНО: тест-хелперы (SandboxEventLogger, RestartButton, содержимое _Sandbox) в боевые сцены НЕ тащить.

## Новые структурные моменты (учесть в новом чате)
- Хват только левой рукой = правый Near-Far Interactor.interactionLayers=Nothing, левый=Default; телепорт правой = отдельный Teleport Interactor.
- UI в VR = uGUI world-space Canvas + TrackedDeviceGraphicRaycaster + EventSystem с XRUIInputModule (НЕ UI Toolkit).
- uGUI-текст: дефолтный шрифт TMP без кириллицы -> для боевого UI нужен шрифт с кириллицей (пока подписи латиницей).
- Правка существующих .cs — ТОЛЬКО на месте (WriteAllText/структурные правки), НЕ delete+create: иначе сменится guid и слетят ссылки в префабах.
- Restart в финале = полный перезапуск игры: ResetProgress() + переход на стартовую сцену; артефакты пересоздаются по данным при заходе.

## СЛЕДУЮЩАЯ ЗАДАЧА: переходы между сценами (fade) + лобби
Обсудить детально в начале нового чата. Открытые вопросы (решить до кода):
1. Архитектура: держать GameManager + загрузчик сцен в постоянной Bootstrap-сцене, город/лобби/викторину грузить additive поверх?
2. Fade: чёрный экран на Canvas (ScreenSpace-Overlay), fade-out/in ~0.4-0.5с при загрузке — ок?
3. С чего начать: связка Bootstrap -> пустое лобби -> песочница города (обкатать переход и перенос артефакта), потом карта в лобби.
Напоминания: XRI сэмплы под 3.2.2 (пакет 3.5.1) — при желании переимпортировать (косметические варнинги симулятора);
  контент по Москве (Экспериментаниум) ещё не написан; ГЕЙТ 'пустой .apk на Quest' по-прежнему в планах.
=== КОНЕЦ АКТУАЛЬНОГО БЛОКА ===


=== ОБНОВЛЕНИЕ 27.09.2026 (ПЕРЕХОДЫ СЦЕН + FADE + ПЕРЕНОС АРТЕФАКТА В РУКЕ) ===

СДЕЛАНО и проверено автотестом в Play (Bootstrap → город → хват → лобби → стенд; 0 ошибок в консоли):

Новые скрипты (GoldenRing.Core):
- ScreenFader.cs — затемнение через чёрную СФЕРУ вокруг камеры (шейдер Assets/_Project/Art/Shaders/ScreenFade.shader — URP, Cull Front + ZTest Always, стерео-корректный). Singleton; FadeOut()/FadeIn(); 0.45с; unscaled time. Материал Assets/_Project/Art/Materials/ScreenFade.mat (_BaseColor, alpha 0 на старте, рендер выключается при alpha≈0).
- SpawnPoint.cs — маркер точки спавна в контентной сцене (позиция + рыскание, gizmo-стрелка).
- PlayerRig.cs — singleton на ПОСТОЯННОМ XR Origin; ссылки Origin + LeftInteractor (левый Near-Far); TeleportTo(target) переносит риг (позиция на пол + yaw).
- SceneLoader.cs — singleton, ЯДРО ПЕРЕХОДОВ. API: GoToLobby() / GoToCity(city) / GoToQuiz(city) / GoToScene(name). Порядок перехода: FadeOut → выгрузка текущей контентной сцены → additive-загрузка новой → SetActiveScene → перенос рига в SpawnPoint → пара кадров на Start спавнеров → FadeIn. Поля loadOnStart + startSceneName для авто-старта.

Новые скрипты (GoldenRing.Interaction):
- LobbyArtifactSpawner.cs — в сцене лобби, на Start. По данным GameManager для каждого города: собран И НЕ поставлен → артефакт ПЕРЕСОЗДАЁТСЯ и force-select в ЛЕВУЮ руку (это и есть перенос в руке под чёрным экраном); поставлен → артефакт на свой стенд (force-select в сокет); не собран → в лобби не показываем.
- ReturnToLobbyButton.cs — ВРЕМЕННАЯ uGUI-кнопка в городе → SceneLoader.GoToLobby(). Позже переедет на диегетичный ТВ робота-гида.

Правки существующих (на месте, guid сохранён):
- ArtifactPickup.cs — добавлен public SetCity(CityData) (артефакт-плейсхолдер получает свой город при пересоздании в лобби).

Bootstrap-сцена (ПОСТОЯННАЯ, тут живёт вся инфраструктура, DontDestroyOnLoad):
- Объект GameManager: GameManager (+GameConfig) + SceneLoader. У SceneLoader сейчас loadOnStart=true, startSceneName=City_Yaroslavl — ВРЕМЕННО, чтобы Play с Bootstrap сразу открывал город для теста среза. Для боевого потока переключить startSceneName на Lobby (или выключить loadOnStart), когда в лобби появится карта.
- PlayerRig — инстанс Assets/_Project/Prefabs/Interaction/PlayerRig.prefab. Риг ОТВЯЗАН от Starter Assets-сэмпла и стал самодостаточным префабом; внутри: FadeSphere (ScreenFader) под Main Camera + компонент PlayerRig (leftHandInteractor = левый Near-Far Interactor). Хват только левой рукой сохранён (правый Near-Far layers=Nothing).
- XR Interaction Manager; XR Interaction Simulator (инстанс сэмпла XRI 3.5.1); EventSystem (+ XRUIInputModule).

Контентные сцены (БЕЗ рига/EventSystem — берут постоянные из Bootstrap):
- Assets/_Project/Scenes/Core/Lobby.unity — Floor, Directional Light, SpawnPoint, Stand_Yaroslavl (префаб), LobbyArtifactSpawner.
- Assets/_Project/Scenes/Cities/City_Yaroslavl.unity — Floor, Directional Light, SpawnPoint, Table, Artifact_Yaroslavl (префаб), ToLobby Canvas (world-space + GraphicRaycaster + TrackedDeviceGraphicRaycaster) с кнопкой TO LOBBY (ReturnToLobbyButton, подпись TMP латиницей).

Build Settings: Bootstrap(0), Lobby(1), City_Yaroslavl(2), BaseScene(3).
Данные: Assets/_Project/Data/Cities/City_yaroslavl.asset — назначены artifactPrefab=Artifact_Yaroslavl И citySceneName=City_Yaroslavl.

ВАЖНО СТРУКТУРНО (учесть в новом чате):
- Риг, XR Interaction Manager, XR Interaction Simulator, EventSystem — ТОЛЬКО в Bootstrap (постоянные). Новые боевые сцены строить БЕЗ них: только окружение + SpawnPoint + интерактив.
- Артефакт НЕ таскаем как объект между сценами — он пересоздаётся по данным GameManager в лобби. Проверено автотестом: после GoToLobby артефакт Ярославля оказывается force-select в левой руке, затем корректно ставится на стенд (placed=1), финал ждёт все 9.
- PlayerRig.prefab — канонический риг для любой сцены, где риг нужен (сейчас только Bootstrap). Песочница Eugen_ArtifactTest тоже переведена на инстанс этого префаба (fade-сфера теперь и там).
- fade-сфера радиусом ~0.15 под Main Camera; в edit mode невидима (материал alpha 0).

СЛЕДУЮЩИЕ ЗАДАЧИ:
- ЛОББИ с картой: клик по городу → SceneLoader.GoToCity(city); замок на закрытых по IsCityUnlocked. После этого переключить loadOnStart Bootstrap на Lobby.
- Обобщить артефакт/стенд/город на 9 городов (пока только Ярославль; артефакты — кубы-плейсхолдеры до моделей Никиты; у остальных CityData назначить artifactPrefab + citySceneName).
- Система викторин (data-driven, Таня) + сцены викторин (SceneLoader.GoToQuiz уже готов).
- Робот-гид (NavMesh follow + ТВ-UI; перенести на ТВ кнопку возврата в лобби).
- Финал по OnAllArtifactsPlaced (карта замыкается → раздвижная дверь → статистика).
- ГЕЙТ: собрать пустой .apk на Quest.
- Для боевого UI — шрифт TMP с кириллицей (сейчас подписи латиницей).
=== КОНЕЦ ОБНОВЛЕНИЯ 27.09.2026 (переходы+fade) ===


=== ОБНОВЛЕНИЕ 27.09.2026 (лобби-меню + Restart + лобби как стартовая сцена + кириллический шрифт) ===

СДЕЛАНО и проверено автотестом в Play (полный цикл, 0 ошибок): Bootstrap → ЛОББИ (авто) → кнопка «В ЯРОСЛАВЛЬ» → город → хват → «В ЛОББИ» (артефакт в руке) → «ПЕРЕЗАПУСК» → чистый сброс.

- Лобби теперь СТАРТОВАЯ сцена: SceneLoader в Bootstrap loadOnStart=true, startSceneName=Lobby (lobbySceneName=Lobby). При Play с Bootstrap игрок попадает в лобби.
- В лобби добавлено меню (world-space Canvas «Lobby Menu Canvas», TrackedDeviceGraphicRaycaster) с двумя кнопками:
  * «В ЯРОСЛАВЛЬ» → GoToCityButton (city=City_yaroslavl) → SceneLoader.GoToCity. У GoToCityButton есть флаг respectUnlock (пока false; для карты включить, чтобы не пускал в закрытые города).
  * «ПЕРЕЗАПУСК» → RestartGameButton → GameManager.ResetProgress() + SceneLoader.GoToLobby() (перезагрузка лобби с чистого листа).
- Новые скрипты (GoldenRing.Interaction): GoToCityButton.cs, RestartGameButton.cs.
- SceneLoader.TransitionRoutine: снята защита «не выгружать, если та же сцена» — теперь GoToScene(текущая) делает полноценную ПЕРЕЗАГРУЗКУ (нужно для Restart из лобби; и для повторного входа в город с карты).
- КИРИЛЛИЦА В UI РЕШЕНА: создан TMP-шрифт Assets/_Project/Art/Fonts/Arial SDF.asset (динамический, из системного Arial; исходник Assets/_Project/Art/Fonts/Arial.ttf). Русские буквы предзагружены в атлас. Использовать этот шрифт для всего боевого TMP-UI. Подписи кнопок в лобби и кнопка города («В ЛОББИ») уже на нём.

СЛЕДУЮЩИЕ ЗАДАЧИ (уточнение):
- Лобби: сделать полноценную КАРТУ городов (сейчас только кнопка в Ярославль). Для клонирования: GoToCityButton на каждый город, respectUnlock=true, замок/подсветка по IsCityUnlocked. Когда карта будет — Restart и разблокировка проверяются на 9 городах.
- Обобщить артефакт/стенд/город на 9 (у остальных CityData назначить artifactPrefab + citySceneName; клонировать City-сцену по шаблону City_Yaroslavl).
- Далее по плану: викторины (Таня), робот-гид, финал, ГЕЙТ .apk на Quest.
=== КОНЕЦ ОБНОВЛЕНИЯ 27.09.2026 (лобби-меню + шрифт) ===


=== ОБНОВЛЕНИЕ 28.09.2026 (карта городов в лобби: маркеры + попап) ===

СДЕЛАНО и проверено автотестом в Play (0 ошибок):

Механика карты (плейсхолдер, без модели — на стене):
- В лобби добавлена стена (объект Map/MapWall, без коллайдера) и 9 маркеров-точек (Map/Marker_<id>, дугой) — по одному на город из GameConfig (порядок по order).
- Маркер = сфера + коллайдер + XRSimpleInteractable (interaction-слой Map) + компонент CityMarker (хранит CityData, подсветка hover через MaterialPropertyBlock _BaseColor). Навёл луч (ЛЮБОЙ рукой) + select → открывается общий попап.
- Общий попап (объект Map Popup, world-space Canvas + TrackedDeviceGraphicRaycaster) + компонент MapController (singleton). Кнопки: «Отправиться в город» (SceneLoader.GoToCity; неактивна + надпись «Скоро», если город закрыт по IsCityUnlocked или нет citySceneName) и «Вернуться к карте» (скрыть). Попап позиционируется у выбранного маркера и разворачивается к игроку. Одновременно открыт максимум один.

ОБЕ РУКИ взаимодействуют с маркерами — но хват артефактов остаётся ТОЛЬКО левой:
- Добавлен XRI interaction-слой Map (индекс 1, бит=2) в Assets/XRI/Settings/Resources/InteractionLayerSettings.asset.
- В PlayerRig.prefab: левый Near-Far interactionLayers = Default+Map (бит 3), правый Near-Far = Map (бит 2). Артефакты на слое Default → правый их не берёт; маркеры на Map → достают обе руки. Проверено: left=3, right=2, marker=2.

Новые скрипты (GoldenRing.Interaction): CityMarker.cs, MapController.cs.

Изменения сцены Lobby:
- Убрана временная кнопка «В Ярославль» (карта её заменяет). Кнопка «ПЕРЕЗАПУСК» (RestartGameButton) оставлена — теперь на отдельном маленьком канвасе справа (Restart Canvas).
- Добавлены Map (стена+маркеры), Map Popup, MapController.

Проверенный сценарий: старт → лобби → навёл на маркер Ярославля (проверено и левой, и правой рукой) → попап «Ярославль», «Отправиться» активна → город грузится. Маркер Москвы (закрыт) → «Отправиться» неактивна, «Скоро». «Вернуться к карте» → попап скрыт. Кириллица в попапе рендерится (шрифт Arial SDF).

ЗАМЕТКИ на будущее:
- Появится модель карты Никиты — маркеры просто перенести на неё (они дети объекта Map); позиции/родителя поменять, логику не трогать.
- Подсветка hover — временный свап цвета; красивый affordance (иконки/обводка/подпись города) потом.
- Когда откроются другие города (по мере прохождения) — «Отправиться» у них включится автоматически (гейт по IsCityUnlocked + наличие citySceneName). Нужно у остальных CityData назначить citySceneName и создать сцены.
=== КОНЕЦ ОБНОВЛЕНИЯ 28.09.2026 (карта) ===


=== ОБНОВЛЕНИЕ 28.09.2026 (фиксы: артефакт data-driven везде + корректный Restart) ===

Проблемы, которые чинили:
- Артефакт в городе не исчезал (был жёстко положен в сцену) — оставался, даже когда уже собран/поставлен.
- После «Перезапуска» артефакт оставался на стенде: сброс шёл при загруженном лобби, и сокет стенда тут же снова перехватывал только что выплюнутый артефакт, помечая его поставленным до перезагрузки.

Сделано:
1) ЕДИНЫЙ спавнер ArtifactSceneSpawner.cs (GoldenRing.Interaction) — заменил LobbyArtifactSpawner (удалён). Висит в КАЖДОЙ контентной сцене, на старте раскладывает единственный артефакт по состоянию GameManager:
   - поставлен → на свой стенд, если стенд есть в этой сцене (лобби);
   - собран и не поставлен → в левую руку (в любой сцене — так «едет» с игроком);
   - не собран → на подставку ТОЛЬКО в сцене своего города (поля sceneCity + pedestalAnchor).
   Гарантия «в руке максимум один». Жёстких артефактов в сценах больше нет.
   - Сцена города City_Yaroslavl: убран жёсткий Artifact_Yaroslavl; добавлены ArtifactAnchor (точка на столе) и объект ArtifactSpawner (ArtifactSceneSpawner, sceneCity=Ярославль, pedestalAnchor=ArtifactAnchor).
   - Лобби: объект спавнера переименован в ArtifactSpawner, компонент заменён на ArtifactSceneSpawner (sceneCity/pedestal пустые — работает на стендах+руке).

2) Корректный Restart: SceneLoader.RestartToLobby() — сброс ПОД чёрным экраном, когда контентная сцена уже выгружена (нет сокета, который перехватит артефакт): fade-out → выгрузить текущую → GameManager.ResetProgress() → загрузить лобби → fade-in. RestartGameButton теперь зовёт RestartToLobby() (свой ResetProgress убран).
   - SceneLoader отрефакторен: общие шаги вынесены в UnloadCurrentContentScene() и LoadContentScene(); TransitionRoutine и RestartRoutine используют их.

Статус: компилируется без ошибок. ЖИВОЙ ПРОГОН — за Женей (проверить сценарии): 
  a) взял в городе → в лобби → вернулся в город: на столе пусто, артефакт в руке;
  b) поставил на стенд → вернулся в город: и стол, и рука пусты (артефакт на стенде в лобби);
  c) Restart с артефактом В РУКЕ → лобби чистое;
  d) Restart с артефактом НА СТЕНДЕ → стенд пуст, прогресс сброшен.
=== КОНЕЦ ОБНОВЛЕНИЯ 28.09.2026 (фиксы артефакта/restart) ===


=== ОБНОВЛЕНИЕ 28.09.2026 (липкий артефакт: нельзя выронить мимо стенда) ===

Дизайн-правило зафиксировано: локомоция — ПРАВАЯ рука, поэтому артефакт берём/держим ТОЛЬКО левой; донести его можно исключительно до своего стенда, выронить куда попало нельзя.

Сделано:
- Новый компонент StickyArtifact.cs (GoldenRing.Interaction) на префабе артефакта (Artifact_Yaroslavl.prefab), рядом с XRGrabInteractable. Пока артефакт лежит на подставке (ещё не взят) — не вмешивается. После взятия рукой: если рука отпускает его НЕ в сокет стенда — через несколько кадров (даём сокету шанс перехватить при постановке) артефакт возвращается силовым SelectEnter в левую руку. Выход в сокет стенда и снятие со стенда рукой не блокируются.
- У XRGrabInteractable артефакта throwOnDetach=false (чтобы не отлетал в кадр отпускания).

Побочно закрыт баг «взял → бросил на пол → вернулся в лобби → снова в руке»: бросить на пол теперь нельзя в принципе, так что фантомных состояний не возникает.

Статус: компилируется без ошибок. ЖИВОЙ ПРОГОН — за Женей:
  - попытаться выронить артефакт в городе/лобби мимо стенда → должен вернуться в левую руку;
  - поставить на стенд → должен нормально встать (не выдёргивается обратно);
  - снять со стенда рукой → берётся;
  - правая рука артефакт по-прежнему не берёт (только телепорт).
Если при постановке артефакт всё же изредка «дёргается» назад в руку — увеличить окно ожидания в StickyArtifact (сейчас 6 кадров).
=== КОНЕЦ ОБНОВЛЕНИЯ 28.09.2026 (липкий артефакт) ===


============================================================
СЛЕПОК АРХИТЕКТУРЫ (актуально на 28.09.2026) — ЧИТАТЬ В ПЕРВУЮ ОЧЕРЕДЬ
Как СЕЙЧАС реально устроена игра. При расхождениях с ранними dated-блоками выше — верен этот слепок.
============================================================

СЦЕНОВАЯ МОДЕЛЬ
- Bootstrap (build index 0) — ПОСТОЯННАЯ сцена, никогда не выгружается, в ней вся инфраструктура.
- Контентные сцены (Lobby, City_*, будущие викторины) грузятся additive ПОВЕРХ Bootstrap; одновременно открыта максимум одна.
- Контентная сцена содержит ТОЛЬКО окружение + один объект SpawnPoint + интерактив. НЕТ рига/менеджеров/EventSystem/камеры — всё из Bootstrap.
- Build Settings: Bootstrap(0), Lobby(1), City_Yaroslavl(2), BaseScene(3, служебная).

ПОСТОЯННАЯ ИНФРАСТРУКТУРА В BOOTSTRAP
- Объект GameManager: GameManager (+ссылка на GameConfig.asset) и SceneLoader.
- PlayerRig — инстанс префаба Prefabs/Interaction/PlayerRig.prefab (XR Origin), singleton PlayerRig, DontDestroyOnLoad.
- XR Interaction Manager; XR Interaction Simulator (инстанс сэмпла XRI 3.5.1); EventSystem (+XRUIInputModule).

ПЕРЕХОДЫ — SceneLoader (Core), singleton
- API: GoToLobby(), GoToCity(CityData), GoToQuiz(CityData), GoToScene(name), RestartToLobby().
- Обычный переход: FadeOut(чёрный) -> выгрузить текущую контентную -> additive-загрузить новую -> SetActiveScene -> перенести риг в SpawnPoint -> 2 кадра на Start спавнеров -> FadeIn. Общие шаги в хелперах UnloadCurrentContentScene()/LoadContentScene().
- RestartToLobby(): FadeOut -> выгрузить текущую -> GameManager.ResetProgress() ПОД чёрным экраном (критично: без загруженного стенда сокет не перехватит артефакт обратно и не пометит его снова поставленным) -> загрузить Lobby -> FadeIn.
- GoToScene(та же сцена) = полная перезагрузка (нужно для Restart и повторного входа в город).
- Старт: loadOnStart=true, startSceneName=Lobby — Bootstrap сам открывает лобби. Проверка состояния сразу после Play может показать пустоту: это ещё идёт первый переход (fade), не баг.

FADE — ScreenFader (Core) + шейдер
- Затемнение = чёрная СФЕРА вокруг Main Camera (в PlayerRig.prefab: Main Camera/FadeSphere). Шейдер GoldenRing/ScreenFade (Art/Shaders/ScreenFade.shader, URP, Cull Front + ZTest Always, стерео). Материал Art/Materials/ScreenFade.mat.
- ДЛИТЕЛЬНОСТЬ и ЦВЕТ — поля Duration/Fade Color на компоненте ScreenFader (FadeSphere), НЕ в шейдере. Менять в префабе PlayerRig.

РИГ И РУКИ — PlayerRig.prefab
- Отвязан от Starter Assets-сэмпла, самодостаточный. Компонент PlayerRig: Origin + LeftInteractor (левый Near-Far). TeleportTo(target) ставит риг в SpawnPoint (позиция + yaw).
- Управление: ЛЕВАЯ рука — предметы (хват), ПРАВАЯ — перемещение (телепорт).

INTERACTION LAYERS (XRI: Assets/XRI/Settings/Resources/InteractionLayerSettings.asset)
- [0] Default — хват предметов; [1] Map — маркеры карты; [31] Teleport — телепорт.
- Левый Near-Far interactionLayers = Default+Map (бит 3): хватает артефакты И жмёт маркеры.
- Правый Near-Far = Map (бит 2): жмёт маркеры, но артефакты НЕ берёт.
- Инвариант: артефакт держим ТОЛЬКО левой; маркеры карты — обе руки.

АРТЕФАКТ — ПОЛНОСТЬЮ DATA-DRIVEN (единственный источник правды = GameManager)
- Жёстко в сценах артефакта НЕТ. В каждой контентной сцене висит ArtifactSceneSpawner (Interaction), на Start пересоздаёт единственный артефакт по состоянию:
   поставлен -> на его стенд, если стенд есть в этой сцене (лобби);
   собран и не поставлен -> в левую руку (в любой сцене -> едет в руке между сценами);
   не собран -> на подставку ТОЛЬКО в сцене своего города (поля sceneCity + pedestalAnchor).
   В руке одновременно максимум один.
- При переходе старый экземпляр уничтожается со сценой, новый воссоздаётся -> иллюзия переноса в руке.
- ArtifactPickup: подбор левой рукой -> GameManager.CollectArtifact; метод SetCity() (спавнер проставляет город клону).
- StandSocket (на узле Socket стенда): фильтр принимает только свой артефакт (по artifactId); при постановке -> PlaceArtifact; по OnProgressReset выталкивает артефакт.
- StickyArtifact (на префабе артефакта): пока держим рукой, выронить куда попало нельзя — если рука отпускает НЕ в сокет, через несколько кадров артефакт силой возвращается в левую руку. Guard от ложного срабатывания при выгрузке/переходе: gameObject.scene.isLoaded + SceneLoader.IsTransitioning (иначе была ошибка StartCoroutine на уничтожаемом объекте, которая на Error Pause вешала паузу). У XRGrabInteractable throwOnDetach=false.

КАРТА В ЛОББИ (плейсхолдер, без модели)
- Объект Map: MapWall (без коллайдера) + 9 маркеров Marker_<id> дугой. Появится модель — маркеры перенести на неё (они дети Map), логику не трогать.
- CityMarker (на маркере): сфера + коллайдер + XRSimpleInteractable (interactionLayers=Map) + CityData. Наведение любой рукой + select -> открыть попап. Hover-подсветка через MaterialPropertyBlock _BaseColor.
- MapController (Interaction, singleton): один общий попап (Map Popup, world-space Canvas + TrackedDeviceGraphicRaycaster). Кнопки: Отправиться в город (SceneLoader.GoToCity; неактивна + надпись Скоро, если IsCityUnlocked=false или нет citySceneName) и Вернуться к карте (скрыть). Позиционируется у маркера, лицом к игроку.
- Restart Canvas: кнопка ПЕРЕЗАПУСК -> RestartGameButton -> SceneLoader.RestartToLobby().

UI / КИРИЛЛИЦА
- Русский текст — TMP-шрифт Art/Fonts/Arial SDF.asset (динамический, из системного Arial, кириллица предзагружена). Дефолтный LiberationSans кириллицу НЕ содержит — для боевого UI брать Arial SDF.

ДАННЫЕ
- GameConfig.asset -> 9 городов (CityData) по order; стартово открыт только order=0.
- City_yaroslavl.asset: id=yaroslavl, artifactId=yaroslavl_rock, artifactPrefab=Artifact_Yaroslavl, citySceneName=City_Yaroslavl. Остальные 8 закрыты, сцены/citySceneName пока не заданы.

КЛЮЧЕВЫЕ СКРИПТЫ
- Core: GameManager, GameConfig, CityData, SceneLoader, ScreenFader, PlayerRig, SpawnPoint.
- Interaction: ArtifactPickup, StandSocket, ArtifactSceneSpawner, StickyArtifact, CityMarker, MapController, GoToCityButton, RestartGameButton, ReturnToLobbyButton (кнопка В ЛОББИ в городе).
- Песочница Eugen_ArtifactTest переведена на PlayerRig.prefab; её тест-хелперы (SandboxEventLogger, RestartButton) — только для песочницы.

ГРАБЛИ / ПРАВИЛА РАБОТЫ ЧЕРЕЗ MCP
- Существующие .cs править ТОЛЬКО на месте (script_apply_edits / WriteAllText) — delete+create меняет guid и рвёт ссылки в префабах/сценах.
- Создание/удаление скриптов и шейдеров вызывает domain reload; MCP-мост может отвалиться на пару шагов — тогда Window -> MCP for Unity -> Reconnect.
- Force-select артефакта: FindAnyObjectByType<XRInteractionManager>().SelectEnter(...) — НЕ через IXRSelectInteractor.interactionManager (у интерфейса его нет).
- Правка префаба через MCP: PrefabUtility.LoadPrefabContents -> изменить -> SaveAsPrefabAsset(root, path) (НЕ SavePrefabAsset — это для инстанса).
- В консоли включён Error Pause: любая ошибка ставит Play на паузу — держим консоль чистой.
- Правка скрипта во время Play применяется только после остановки Play (перекомпиляция).
============================================================
КОНЕЦ СЛЕПКА АРХИТЕКТУРЫ 28.09.2026
============================================================


=== ДИЗАЙН-РЕШЕНИЕ 29.09.2026 (новый порядок прохождения + робот-гид EVE) — ЕЩЁ НЕ В КОДЕ ===
Подтверждено с Таней/Женей. Это ДИЗАЙН, код пока по старой схеме (см. СЛЕПОК выше). Реализовать при работе над роботом/викторинами.

НОВЫЙ ПОРЯДОК ПРОХОЖДЕНИЯ ГОРОДА:
- спавн в городе -> робот (EVE) ведёт и рассказывает -> игрок находит артефакт (в левую руку) -> робот подъезжает и на экране предлагает викторину -> переход в СЦЕНУ ВИКТОРИНЫ.
- В сцене викторины: игрок кладёт артефакт в спец. ДЕРЖАТЕЛЬ (НЕ стенд!). Это НЕ вызывает GameManager.PlaceArtifact (постановка на стенд = только лобби, для финала). Держатель лишь ЗАПУСКАЕТ викторину (пример: планеты Солнечной системы появляются только после того, как артефакт положен).
- По прохождении викторины -> GameManager.CompleteQuiz(city) (открывает следующий город). Игрок ЗАБИРАЕТ артефакт из держателя обратно в руку.
- Возврат в ЛОББИ -> ставит артефакт на СТЕНД (это и есть PlaceArtifact, идёт в финал).
ОГРАНИЧЕНИЯ НАВИГАЦИИ:
- В лобби НЕЛЬЗЯ вернуться, не пройдя викторину (лобби гейтится прохождением текущей викторины, а не отказом стенда).
- Из сцены викторины МОЖНО вернуться в город и заново переслушать рассказ робота.
СЛЕДСТВИЕ ДЛЯ КОДА (что перестроить): старая кнопка 'В лобби' в городе больше не ведёт в лобби напрямую до викторины; SceneLoader.GoToQuiz уже есть; нужен QuizHolder (держатель, не Socket стенда) + активация викторины по факту постановки; из квиза выходы: 'назад в город' (доступен всегда) и 'в лобби' (только после прохождения).

РОБОТ-ГИД EVE (подтверждённые факты; поведение детализируем отдельно):
- Модель: Assets/_Project/Prefabs/Guide/Robot.fbx (белый робот EVE). Диегетичный UI — на её экране, не отдельные плоские панели.
- Робот делается ПЕР-СЦЕННО (свой в каждой сцене города), не как единый переносимый спутник. NavMesh печётся в каждой сцене.
- Follow: едет за игроком, держит дистанцию ~1 м, доезжает и ОСТАНАВЛИВАЕТСЯ (не липнет). При телепорте игрока перестраивает путь.
- Go-to-exhibit: закончил рассказ -> сразу едет к следующему экспонату, но НОВЫЙ рассказ НЕ начинает, пока игрок не подойдёт. У экспоната ходьбу игрока НЕ блокируем.
- Pre-placed: уже стоит у экспоната, ВСЕГДА повёрнута к игроку (чтобы было видно, что ждёт), рассказ — по подходу.
- Offer-quiz: после взятия артефакта подъезжает к игроку, на экране кнопка 'Пройти викторину' -> GoToQuiz(city).
ОТКРЫТО (решаем в чате):
- Ревизит уже пройденного города: игрок спавнится 'в комнатах со стендами'(?), EVE стоит с кнопкой 'провести рассказ ещё раз'. Детали спавна/комнат — уточняются.
- Объём робота к прототипу: Вариант 1 (минимум: Follow + 1 Go-to-exhibit + Offer-quiz) vs Вариант 2 (полный, +Pre-placed +неск. остановок) — выбор за Женей.
=== КОНЕЦ ДИЗАЙН-РЕШЕНИЯ 29.09.2026 ===

=== УТОЧНЕНИЯ 29.09.2026 (робот/город — к дизайн-блоку выше) ===
- ВЫБРАН Вариант 2 (Follow + Go-to-exhibit + Pre-placed + Offer-quiz). Не хватит времени — сокращаем число остановок; движок всё равно data-driven под список остановок.
- СТРУКТУРА ГОРОДА: спавн ПЕРЕД зданием (фасад; вокруг почти не походить) -> у двери телепорт-ВЕЙПОИНТ в стиле SteamVR (светящееся кольцо-якорь) -> телепорт на него переносит ВНУТРЬ, в комнаты с экспонатами. Тур EVE идёт внутри.
- РЕВИЗИТ пройденного города: спавн сразу ВНУТРИ (комнаты с экспонатами), EVE стоит с кнопкой 'Провести рассказ ещё раз' (автотур сам не стартует).
- АРТЕФАКТ ПРЯЧЕМ: не на виду. Активируется ТОЛЬКО после рассказа: EVE ~'сейчас спрячу' -> короткое затемнение (существующий ScreenFader) -> артефакт включается в укрытии, игрок ИЩЕТ. Причина: отдельный критерий за квест-поиск. (Ранее было 'на подставке после рассказа' — ОТМЕНЕНО.)
=== КОНЕЦ УТОЧНЕНИЙ 29.09.2026 ===

=== РОБОТ-ГИД: ДВИЖОК СОБРАН И АВТОТЕСТ ПРОШЁЛ (29.09.2026, песочница) ===
Namespace GoldenRing.Guide (Assembly-CSharp):
- Assets/_Project/Scripts/Guide/GuideStop.cs — маркер остановки тура в сцене: order, mode {Follow|GoToExhibit|PrePlaced}, narration (AudioClip; пусто=бип), placeholderDuration, approachRadius (ПУБЛИЧНЫЙ, настраивается на каждый экспонат отдельно), facesPlayerWhileWaiting, hideArtifactAfter. Гизмо рисует радиус подхода.
- Assets/_Project/Scripts/Guide/RobotGuide.cs — конечный автомат на объекте EVE (NavMeshAgent+AudioSource). Собирает GuideStop по order и проходит их:
   Follow: едет за игроком, дистанция followStandoff (~1 м) с гистерезисом followResumeMargin, доезжает и встаёт (не липнет);
   GoToExhibit: подъезжает к остановке, ждёт подхода игрока (< approachRadius), лицом к игроку, затем рассказ;
   PrePlaced: если не на точке — Warp на неё ('гид уже там'), всегда лицом к игроку, рассказ по подходу;
   после hideArtifactAfter — beat 'прячу артефакт': реплика/бип -> ScreenFader.FadeOut -> артефакт в hidePoint + SetActive(true) -> FadeIn -> поиск; подсказка каждые hintDelay (по умолч. 60 с);
   после сбора артефакта -> OfferQuiz: подъезжает к игроку, включает экранный UI с кнопкой -> RobotGuide.GoQuiz() -> SceneLoader.GoToQuiz(city).
ВАЖНО (грабли, уже учтено): факт сбора берём из GameManager.IsArtifactCollected(city), а не только из события OnArtifactCollected — иначе гонка (OnEnable робота раньше Awake GameManager) и событие теряется. Подписка продублирована в Start.
Песочница Assets/_Project/Scenes/_Sandbox/Eugen_GuideTest.unity (сделана 'Сохранить как' из Eugen_ArtifactTest): XR-риг+симулятор+менеджер+EventSystem+fade+пол+свет+GameManager(Sandbox). NavMesh запечён (NavMeshSurface на Floor). EVE = Robot.fbx (импорт-масштаб был ~76, отмасштабирован до ~1.6 м, ногами на пол) + NavMeshAgent+AudioSource+RobotGuide; экран = world-space Canvas 'EVE Screen Canvas' + кнопка 'Пройти викторину' (legacy Text, шрифт Arial, кириллица; TrackedDeviceGraphicRaycaster для луча) — включается только в OfferQuiz. Спрятанный артефакт = переиспользован инстанс Artifact_Yaroslavl (выключен на старте, активируется в HidePoint). Остановки: Stop_Intro (Follow, order0), Stop_A_GoTo (GoToExhibit, order1), Stop_B_PrePlaced (PrePlaced, order2, hideAfter).
АВТОТЕСТ (Play, автоматически, approachRadius временно =50 чтобы пройти без ручного подхода): цепочка Follow->GoToExhibit->PrePlaced->beat прячу->артефакт активирован->(имитирован сбор через GameManager)->тур завершён->подъезд к игроку->кнопка викторины показана. 0 ошибок в консоли. Радиусы/длительности возвращены в боевые (2 м; intro 5с, экспонаты 4с).
ЖИВОЙ ТЕСТ ЗА ЖЕНЕЙ (в XR Device Simulator, ещё не делали): открыть Eugen_GuideTest, войти в Play и ногами проверить — EVE следует на ~1 м и встаёт; подходишь к экспонату -> рассказ начинается; EVE в PrePlaced повёрнута к тебе; после пряток ФИЗИЧЕСКИ найти и взять артефакт ЛЕВОЙ рукой (автотест собирал форсированно) -> EVE подъезжает -> клик лучом по кнопке 'Пройти викторину' (в песочнице сцены викторины нет -> уйдёт в лог). Подсказку можно проверить, снизив hintDelay.
ДАЛЬШЕ (по роботу): вживить в боевую сцену города (фасад + телепорт-вейпоинт + интерьер), сделать ревизит с кнопкой 'Провести рассказ ещё раз', подложить реальные аудиоклипы, определиться с конвенцией PrePlaced (отдельный экземпляр vs Warp). Модель EVE — плейсхолдер-визуал, финальную поставит/поправит Никита.
=== КОНЕЦ (робот-гид движок) 29.09.2026 ===

=== РОБОТ-ГИД: ЖИВОЙ ТЕСТ ПРОШЁЛ + ПРАВКИ (29.09.2026, Женя) ===
Проверено в XR-симуляторе, работает. Что чинили после первого прогона:
- ПОВОРОТ: ориентацией рулит только RobotGuide (agent.updateRotation=false). В движении смотрит по ходу (FaceMove), стоя — на игрока (FaceTowards). Разворот модели правит поле faceYawOffset. Для нашей модели Robot.fbx верное значение = 90 (модель повёрнута на четверть). Дефолт в скрипте выставлен 90 -> будущие EVE в других городах сразу правильные.
- ТЕЛЕПОРТ между точками убран: был Warp в режиме PrePlaced. Тур теперь Follow -> GoToExhibit -> GoToExhibit (последняя прячет артефакт), EVE едет ногами.
КОНВЕНЦИЯ PrePlaced ЗАФИКСИРОВАНА: в комнатах с экспонатами робот ОДИН (EVE). Поэтому PrePlaced используем ТОЛЬКО для стартовой точки — EVE стоит у входа в здание рядом с телепорт-вейпоинтом, повёрнута к игроку и может что-то сказать; дальше едет туром (Follow/GoToExhibit). Мид-тур PrePlaced не применяем (это и давало телепорт одного робота).
Статус: движок робота-гида собран и подтверждён живым тестом. Осталось по роботу: вживить в боевую сцену города (фасад+вейпоинт+интерьер), реальные аудиоклипы, ревизит 'провести рассказ ещё раз'.
=== КОНЕЦ (робот-гид: живой тест) 29.09.2026 ===


=== ОБНОВЛЕНИЕ 02.10.2026 (КОСТРОМА — первый боевой город на новой схеме + стык спавнера с роботом) ===
ПЛАТФОРМА: решено делать билд под HTC Vive (десктопный VR), не Quest. Хват двумя руками там допустим — оставили как есть (левая рука под предметы — по-прежнему рабочий инвариант).

ГЛАВНОЕ — сведён стык «спрятанный артефакт ↔ data-driven спавнер» (в бою они раньше конфликтовали):
- ArtifactSceneSpawner (правка НА МЕСТЕ, guid сохранён): добавлены поля hideForGuide(bool) и guide(RobotGuide).
  Ветка «не собран, свой город»: если hideForGuide=true — артефакт спавнится ДЕАКТИВИРОВАННЫМ в hidePoint робота,
  и ссылка отдаётся RobotGuide.hiddenArtifact (робот сам показывает его после рассказа — квест-поиск).
  Если false — старое поведение (кладёт на pedestalAnchor). RobotGuide НЕ менялся (hidePoint/hiddenArtifact уже public).
- Проверено в Play: спавнер создаёт ровно ОДИН Artifact_Kostroma(Clone) active=False и проставляет его EVE.hiddenArtifact.

EVE СТАЛА ПРЕФАБОМ: Assets/_Project/Prefabs/Guide/EVE.prefab (из инстанса песочницы, SaveAsPrefabAssetAndConnect).
- Внутри: модель Robot_Visual, Screen, EVE Screen Canvas(+QuizButton), RobotGuide (screen/screenUI/quizButton связаны),
  NavMeshAgent, AudioSource, + NavMeshModifier(ignoreFromBuild=true) — чтобы EVE НЕ запекалась в навмеш как препятствие.
- city/hidePoint/hiddenArtifact в ПРЕФАБЕ пустые (назначаются по городу на инстансе).
- ГРАБЛИ: назначая эти ссылки на инстансе EVE, ОБЯЗАТЕЛЬНО EditorUtility.SetDirty(rg)+SaveScene — иначе при
  реимпорте префаба сбрасываются в null (ловили в этой сессии).

ПРЕФАБЫ КОСТРОМЫ:
- Artifact_Kostroma.prefab (копия ярославского): сфера-«метеорит», SphereCollider, серый материал
  Art/Materials/Artifact_Kostroma.mat, ArtifactPickup/StickyArtifact/XRGrabInteractable, city=City_kostroma.
- Stand_Kostroma.prefab (копия ярославского), StandSocket.city=City_kostroma.
- City_kostroma.asset: проставлен artifactPrefab=Artifact_Kostroma (остальное уже было: order=1,
  citySceneName=City_Kostroma, quizSceneName=Quiz_Kostroma, artifactId=kostroma_meteorite «Обломок метеорита»).

СЦЕНА City_Kostroma.unity (ПЕРВЫЙ боевой город по дизайну 29.09, грейбокс — модели Никиты позже):
- Структура: Плаза (TeleportationArea) → Фасад с дверным проёмом + Купол-намёк на планетарий →
  телепорт-ВЕЙПОИНТ (TeleportationAnchor «TeleportWaypoint», светящийся pad, AnchorDest внутри) → интерьерный зал со стенами.
- Экспонаты: Exhibit_A/B (подиум+планета), Exhibit_Projector (центр). HidePoint — дальний угол на ящике (там прячется артефакт).
- EVE (инстанс префаба) + остановки: Stop_Intro(Follow,0), Stop_A_GoTo(GoToExhibit,1), Stop_B_GoTo(GoToExhibit,2, hideArtifactAfter=true).
- ArtifactSpawner: sceneCity=kostroma, hideForGuide=true, guide=EVE.
- NavMeshSurface на InteriorFloor запечён; EVE и Купол исключены из запекания (NavMeshModifier ignoreFromBuild).
- БЕЗ рига/менеджеров/EventSystem/камеры — всё постоянное из Bootstrap. Телепорт-поверхности на слое Default (value=1),
  как рабочий Floor песочницы.

СТАБ ВИКТОРИНЫ Quiz_Kostroma.unity (временно, до боевой викторины Тани — планеты/сахар):
- Scripts/Quiz/QuizStubController.cs: CompleteAndReturn() = CompleteQuiz(city)+GoToLobby; BackToCity() = GoToCity(city).
- Пол+телепорт, SpawnPoint, держатель (косметика), ArtifactSceneSpawner (collected→в руку), мир-UI с 2 кнопками.
- КОГДА ПРИДЁТ ПАКЕТ ТАНИ: заменить стаб на реальную викторину (держатель запускает викторину; по успеху — CompleteQuiz),
  добавить выходы «назад в город» (всегда) и «в лобби» (только после прохождения).

BUILD SETTINGS ИСПРАВЛЕНЫ: раньше на индексе [0] стояла песочница Eugen_GuideTest(!). Теперь:
  [0]Bootstrap [1]Lobby [2]City_Yaroslavl [3]City_Kostroma [4]Quiz_Kostroma; Eugen_GuideTest отключён (галка снята).
  Песочницы в билд не идут.

ЛОББИ: добавлен Stand_Kostroma (справа от ярославского, x+1.0). Карта городов уже генерит маркер Костромы из GameConfig
  — «Отправиться» включается по IsCityUnlocked.
  ВРЕМЕННО: объект TEST_DebugUnlock (Scripts/_Debug/DebugUnlock.cs, unlockUpToOrder=1) открывает Кострому для теста
  (боевой викторины Ярославля ещё нет). !!! УДАЛИТЬ или выключить (runOnStart=false) перед сдачей — ломает линейную прогрессию.

ПРОВЕРЕНО В PLAY (0 ошибок; только косметический haptic-варнинг симулятора XRI):
  Bootstrap → лобби(авто) → DebugUnlock открыл Кострому → GoToCity(Кострома) → спавнер спрятал артефакт и отдал EVE →
  (сымитирован сбор) → GoToQuiz → стаб, артефакт «приехал» в руке → CompleteAndReturn → открылся Переславль →
  возврат в лобби, артефакт в руке.

ЖИВОЙ ТЕСТ ЗА ЖЕНЕЙ (headless не покрывает — нужен XR-симулятор/Vive):
  - Тур EVE ногами: Follow → подход к Exhibit_A → рассказ → Exhibit_B → рассказ → beat «прячу» (затемнение) →
    найти метеорит в углу у ящика → взять рукой → EVE подъезжает → кнопка «Пройти викторину».
  - Телепорт-вейпоинт: встать на плазе снаружи, телепортнуться на светящийся pad у двери → перенос внутрь зала.
  - В лобби: поставить метеорит на Stand_Kostroma; проверить Перезапуск.

СЛЕДУЮЩЕЕ:
  - Вставить боевую викторину Тани (планеты) вместо стаба Quiz_Kostroma; реализовать держатель (кладём артефакт → запуск викторины).
  - Ретрофит Ярославля на новую схему (робот в сцене) по образцу Костромы, затем клонировать на остальные города.
  - Реальные аудиоклипы рассказа EVE; модели Никиты (здание планетария, метеорит, финальная EVE).
  - ГЕЙТ: собрать билд под Vive как можно раньше.
=== КОНЕЦ ОБНОВЛЕНИЯ 02.10.2026 (Кострома) ===


=== ОБНОВЛЕНИЕ 02.10.2026 (КОСТРОМА v2: кубическая комната Никиты + зонный гид EvaZoneGuide + озвучка) ===
Кострома переведена с грейбокс-фасада (блок выше) на БОЕВОЙ вид: кубическая комната-витрина (модель Никиты room.fbx) с тремя экспонатами на стендах. Старый грейбокс-подход устарел, верно всё ниже.

КОМНАТА (room.fbx, inst 'room'):
- Была «пустая внутри»: материалы пола/стен/потолка стояли на _Cull=2 (задние грани), комната полая → изнутри не рисовалась. ФИКС: три материала (Prefabs/room/Materials/*) переведены в _Cull=0 (двусторонние) + doubleSidedGI. Коллайдеров не было → у импортёра room.fbx включён addCollider (MeshCollider на пол/стены/потолок).
- Предупреждения «self-intersecting polygon discarded» (по 1 на меш) — это геометрия из Блендера (n-gon самопересечение), Unity их не чинит; на вид не влияет, это warning. Чистится в Блендере (Merge by Distance + Triangulate + Recalculate Normals).

ОЗВУЧКА: Assets/_Project/Audio/Guide/Kostroma/Kostrom1..6.mp3. Порядок рассказа ИЗМЕНЁН: Цейс → метеорит → Спутник.
  Kostrom1=вступление (у входа), Kostrom2=Малый Цейс, Kostrom3=метеорит, Kostrom4=Спутник-1, Kostrom5=задание «найди гостя из космоса», Kostrom6=финал/викторина.

НОВЫЙ КОНТРОЛЛЕР EvaZoneGuide.cs (GoldenRing.Guide) — зонно-триггерный гид (заменяет RobotGuide для схемы с точками/зонами):
- Остановка = {evaPoint (куда встать EVE), zone (Collider, игрок вошёл → реплика), clip, teleportToPoint}.
- Поток: intro (StartEVAPoint + ZoneToActivateTalkWithEVA + Kostrom1) → экспонаты по порядку (EVApoint2/3/4 + ZoneToActivateEva2/3/4 + Kostrom2/3/4; Цейс teleportToPoint=true — прыжок снаружи внутрь) → задание (Kostrom5) → ждём сбор метеорита → финал (Kostrom6) → кнопка «Пройти викторину» → GoToQuiz.
- Движение простым transform (БЕЗ навмеша — на импортной комнате не печём). Детект игрока в зоне — через collider.bounds.Contains (без триггер-callback'ов).
- ГРАБЛИ (пофикшено): EvaZoneGuide стартовал до переноса рига загрузчиком — риг был в origin (внутри зоны разговора) → intro срабатывал сразу. Фикс: в начале Start ждём `while(SceneLoader.Instance.IsTransitioning) yield` (+кадр), только потом слушаем зоны. Проверено в Play — больше не срабатывает преждевременно.

EVE в сцене: старый RobotGuide и NavMeshAgent ВЫКЛЮЧЕНЫ (enabled=false), добавлен EvaZoneGuide (заполнены intro/stops/клипы/город/экран). Кнопка викторины перепривязана на EvaZoneGuide.GoQuiz. EVE стартует на StartEVAPoint.

АРТЕФАКТ: теперь это МОДЕЛЬ НИКИТЫ 'meteor (1)' в комнате (не спавнится). На неё навешано: Rigidbody(kinematic,noGravity)+BoxCollider+XRGrabInteractable(layers=Default)+ArtifactPickup(city=kostroma)+StickyArtifact. Хват левой рукой → CollectArtifact (проверено в Play). Старый ArtifactSceneSpawner в сцене ВЫКЛЮЧЕН (enabled=false) — чтобы не плодил второй (сфера-плейсхолдер).
  НЕСТЫКОВКА на будущее: в лобби/викторине артефакт по-прежнему data-driven Artifact_Kostroma (сфера), а в комнате — метеорит Никиты. Чтобы свести — позже назначить Artifact_Kostroma.prefab модель метеорита (или заменить префаб).

ТЕЛЕПОРТ: TeleportationAnchor 'TeleportWaypoint'.teleportAnchorTransform = SpawnpointInRoom (-2.86,0.32,8.48) — платформа телепортирует игрока внутрь комнаты.

МАРКЕРЫ: зоны ZoneToActivate* → isTrigger=true, MeshRenderer выключен. Точки EVApoint*/StartEVAPoint → MeshRenderer и Collider выключены (чистые маркеры).

ПРОВЕРЕНО В PLAY (0 ошибок, только haptic-варнинг симулятора):
  Bootstrap→лобби→DebugUnlock открыл Кострому→GoToCity→EVE ждёт (intro НЕ сработал раньше)→игрок в ZoneToActivateTalkWithEVA→intro играет; хват метеорита→сбор артефакта засчитан.

ЖИВОЙ ТЕСТ ЗА ЖЕНЕЙ (headless не покрывает ходьбу/хват/телепорт в VR):
  спавн снаружи → подойти в зону разговора (intro) → встать на телепорт-платформу → перенос в комнату (SpawnpointInRoom) → подходить к зонам Цейс/метеорит/Спутник (EVE рассказывает по порядку) → по заданию найти и взять метеорит левой рукой → финал → кнопка «Пройти викторину» → Quiz_Kostroma → лобби, поставить артефакт на Stand_Kostroma.

ОСТАЛОСЬ ПО КОСТРОМЕ: свести вид артефакта (метеорит vs сфера) лобби/викторины с комнатой; заменить стаб Quiz_Kostroma на викторину Тани (планеты); проверить NavMesh не нужен (EVE двигается transform'ом). TEST_DebugUnlock в лобби — удалить перед сдачей.
=== КОНЕЦ ОБНОВЛЕНИЯ 02.10.2026 (Кострома v2) ===


=== ОБНОВЛЕНИЕ 03.10.2026 (EVE ходит по путевым точкам внутри комнаты) ===
Уточнение к маршруту EVE: прыжок (teleport) — ТОЛЬКО на вход в комнату, внутри EVE ИДЁТ пешком между экспонатами (между секциями есть проход).
- В EvaZoneGuide добавлен флаг useNavMesh (bool). useNavMesh=false → EVE идёт по ПРЯМОЙ между путевыми точками (evaPoint'ы задают маршрут вручную); useNavMesh=true → движение по NavMesh (agent) для городов со связным полом.
- Поддержка точек-ВЕЙПОИНТОВ: остановка без zone и без clip = EVE просто проходит её и идёт дальше (в цикле: WaitPlayerInZone только если zone!=null, Narrate только если clip!=null).
- Женя добавил промежуточные точки EVApoint3.1 (0.98,8.34) и EVApoint3.2 (4.92,8.32) — ведут через проход из секции метеорита в секцию Спутника.
- Маршрут Костромы (useNavMesh=false): Цейс (EVApoint2, teleport=true — ВХОД) → метеорит (EVApoint3, пешком) → wp EVApoint3.1 → wp EVApoint3.2 → Спутник (EVApoint4, пешком). Отрезки между всеми точками проверены рейкастом на y=0.9 — стен на пути нет.
- ГРАБЛИ (Unity): после правки скрипта редактор ~минуту не перезагружал домен (новое поле не появлялось в сборке), т.к. окно Unity было не в фокусе. Лечится: кликнуть по окну Unity, чтобы оно дожало компиляцию. ForceSynchronousImport/RequestScriptReload через MCP не помогли.
- Телепорт-платформа: починка (colliders у якоря) подтверждена — переносит на SpawnpointInRoom. Тег Player на риге стоит.
Проверено в Play: город грузится без ошибок, EVE ждёт на старте (intro не срабатывает раньше). Полную ходьбу по маршруту смотреть в шлеме.
=== КОНЕЦ ОБНОВЛЕНИЯ 03.10.2026 (ходьба EVE) ===


=== ОБНОВЛЕНИЕ 03.10.2026 (правки по итогам теста в шлеме) ===
1) ХВАТ АРТЕФАКТА — НА ПРАВУЮ РУКУ (на Vive левая занята ходьбой). Правка только в префабе PlayerRig (без кода):
   Left Controller/Near-Far interactionLayers=2 (Map, артефакты НЕ берёт); Right Controller/Near-Far=3 (Default+Map, берёт);
   PlayerRig.leftHandInteractor («держащая рука») = Right Controller Near-Far. Имя поля историческое — теперь это ПРАВАЯ рука.
2) АРТЕФАКТ НЕЛЬЗЯ ВЗЯТЬ ДО РАССКАЗА. EvaZoneGuide: SetArtifactGrabbable(false) на старте, SetArtifactGrabbable(true) ПОСЛЕ taskClip.
   Проверено в Play: на входе в город meteor grab.enabled=False.
3) NAVMESH ТОЛЬКО ПО МАРШРУТУ. Убран старый NavMeshSurface (collect=All) с грейбокс-InteriorFloor. Добавлен объект EvaNavZone
   (origin, NavMeshSurface, collectObjects=Volume, center≈(3.1,1,8.25), size≈(9.5,4,6.5)) — запекает только коридор маршрута
   EVE (~53 вершины вместо 2844). EVE всё равно ходит по точкам (useNavMesh=false), навмеш минимальный.
Все решения занесены в CITY_RECIPE.md (§5, §15 схема управления Vive, §16 navmesh).
=== КОНЕЦ ОБНОВЛЕНИЯ 03.10.2026 (правки теста) ===


=== ОБНОВЛЕНИЕ 03.10.2026 (КОЛЛАЙДЕРЫ КОСТРОМЫ: здание, зал, забор + уборка грейбокса) ===
Проблема: игрок проходил сквозь стены. У стен зала был только импортный MeshCollider (room.fbx) — ненадёжно для continuous-move/телепорт-луча; наружное здание и забор рабочих коллайдеров не имели; во дворе/зале оставался грейбокс с дублирующими коллайдерами и телепорт-полом.

СДЕЛАНО — все коллайдеры ПРОСТЫЕ BoxCollider:
- ЗАБОР: BoxCollider (auto-fit) на 4 сегмента ограды KostromaMainBuilding/01_2, 01_2.002, 01_2.003, 01_2.004. Замкнутый прямоугольник двора ~27x31 м, высота ~2.1 м — игрок не покидает территорию.
- ЗДАНИЕ: новый корневой объект «Colliders» с кольцом из 4 боксов по периметру node_0 (Building_North/South/West/East): footprint x[-7.1..10.3], z[1.6..14.9], высота 0..5. Со двора нельзя войти в здание сквозь стену. Вход — телепортом с pad, поэтому кольцо замкнутое (дверь для ходьбы не нужна).
- ЗАЛ (room.fbx): 4 бокса по периметру внутренних стен (RoomWall_North/South/West/East) в «Colliders»: x[-3.49..9.93], z[5.72..10.92], высота 0.1..3.5. Изнутри больше не пройти сквозь стены — это и была жалоба «у стен есть коллайдер, но проходит».
- Родные коллайдеры комнаты НЕ трогали: пол room/Cube.001 остаётся walkable + TeleportationArea; боксы стен добавлены поверх.

УБОРКА ГРЕЙБОКСА (SetActive(false), обратимо):
- Выключены: InteriorFloor (старый серый пол с дублирующим TeleportationArea — конфликтовал с полом комнаты), Exhibit_A/B/Projector (серые тумбы — настоящие экспонаты в room), Facade(+дети), Dome, Stop_Intro/Stop_A_GoTo/Stop_B_GoTo (маркеры неиспользуемого RobotGuide). Wall_Back/SideL/SideR уже были выключены.
- Выключен лишний Collider на маркере room/SpawnpointInRoom (объект оставлен — он цель телепорта).
- Plaza ОСТАВЛЕНА (на ней спавн + её TeleportationArea; пометка в CITY_RECIPE §14 про удаление Plaza устарела — исправлено).

ПРОВЕРЕНО: компиляция/консоль чистые (0 ошибок). Рейкасты: стены зала ловятся RoomWall_*, двор→здание упирается в Building_South, двор→забор в сегменты ограды, пол под комнатой есть. Play из Bootstrap → GoToCity(Кострома): сцена грузится additive без ошибок, «Colliders» (8 боксов) на месте.

ВАЖНО про VR: BoxCollider блокируют continuous-move (DynamicMoveProvider + CharacterController) и телепорт-луч (нельзя прицелиться сквозь бокс). ФИЗИЧЕСКУЮ room-scale ходьбу (движение головой) коллайдеры НЕ останавливают — ограничение VR, лечится boundary/vignette, не коллайдерами.

ЖИВОЙ ТЕСТ ЗА ЖЕНЕЙ (в шлеме): стиком в стену зала/здания/забор — упираться; телепортом «сквозь стену» в окно — луч блокируется стеной; телепорт с pad внутрь — работает.
=== КОНЕЦ ОБНОВЛЕНИЯ 03.10.2026 (коллайдеры Костромы) ===


=== ОБНОВЛЕНИЕ 03.10.2026 (ПОЧЕМУ ИГРОК ПРОХОДИЛ СКВОЗЬ СТЕНЫ + HeadCollisionPushback) ===
Жалоба: после установки коллайдеров игрок всё равно проходит сквозь стены и даже сквозь свежесозданный тестовый куб.

ДИАГНОЗ (проверено в Play):
- CharacterController рига ВКЛЮЧЁН; матрица столкновений слоёв в норме (слой игрока 2 Ignore Raycast сталкивается с Default) — СЛОЙ НЕ ВИНОВАТ.
- Прямой CharacterController.Move упирается в коллайдер — коллизия физически работает.
- Локомоция стиком (DynamicMoveProvider → MoveRig → CharacterController.Move) тоже упирается — стик коллизию уважает.
- НО движение ПОЗОЙ КАМЕРЫ (физическая room-scale ходьба и перемещение в XR Device Simulator) идёт МИМО CharacterController → проходит сквозь любые коллайдеры (в т.ч. свежий куб). Это и видел тестер (Game-вью = симулятор). Это фундаментальное свойство VR: трекинг головы коллайдерами не блокируется.

РЕШЕНИЕ: Assets/_Project/Scripts/Interaction/HeadCollisionPushback.cs (GoldenRing.Interaction) — висит на PlayerRig.prefab (origin+controller назначены; collisionLayers = всё кроме слоя игрока). Каждый LateUpdate: капсула CharacterController едет под голову (высота по камере), и если голова внутри геометрии — риг выталкивается по горизонтали (OverlapCapsule + Physics.ComputePenetration, только XZ). При движении стиком penetration нет → скрипт бездействует (без двойного хода). Теперь голову нельзя просунуть сквозь стену при ЛЮБОМ способе движения.

ПРОВЕРЕНО в Play (покадровая имитация физ-ходьбы): голова встаёт у стены зала (z 8→10.64 при стене 10.9), у наружного здания (x 13→10.63 при стене 10.3), у забора (z→-12.22 при заборе -12.6). Консоль чистая.

ЖИВОЙ ТЕСТ ЗА ЖЕНЕЙ (шлем/симулятор): пройти ногами/варпом в стену зала, в здание и в забор — голова должна упираться. Если на стыке стен дёргает/застревает — поднять solverIterations (сейчас 3) или radius капсулы CharacterController (сейчас 0.1).
=== КОНЕЦ ОБНОВЛЕНИЯ 03.10.2026 (HeadCollisionPushback) ===


=== ИТОГ СЕССИИ 03.10.2026 (вечер) + СЛЕДУЮЩИЙ ШАГ ===
СДЕЛАНО в этой сессии (детали — в двух блоках выше):
1) Коллайдеры Костромы простыми BoxCollider: забор (4 сегмента, замкнут), наружное здание node_0 (кольцо Building_*), зал room.fbx (RoomWall_*). Уборка грейбокса (InteriorFloor/Exhibit_*/Facade/Dome/Stop_* выключены; лишний коллайдер SpawnpointInRoom снят). Plaza оставлена.
2) Найдена и устранена причина «игрок проходит сквозь стены»: движение позой камеры (физ-ходьба/XR Device Simulator) шло мимо CharacterController. Добавлен HeadCollisionPushback.cs на PlayerRig.prefab — выталкивает риг при заходе головы в геометрию. Работает для зала/здания/забора, проверено в Play.
Файлы изменены: City_Kostroma.unity, PlayerRig.prefab, новый Scripts/Interaction/HeadCollisionPushback.cs. Доки: CITY_RECIPE §2/§11/§14/§15, DEV_PROGRESS (эти блоки).

СЛЕДУЮЩАЯ ЗАДАЧА (новый чат): разработка следующего города по образцу Костромы.
Порядок чтения: PROJECT_BRIEF.md → CITY_RECIPE.md (особенно §12 пошаговый рецепт + §15 управление + §16 navmesh) → свежие блоки DEV_PROGRESS. Следующий город по order — Переславль-Залесский (order=2, City_pereslavl). Сначала получить от Никиты модель здания/комнаты. НЕ забыть: у стен — простые BoxCollider (не только импортный MeshCollider), HeadCollisionPushback уже на риге (общий для всех городов). Перед сдачей убрать TEST_DebugUnlock из лобби.
=== КОНЕЦ ИТОГА 03.10.2026 ===


=== ОБНОВЛЕНИЕ 03.10.2026 (ФИНАЛ: замыкание кольца + дверь + экран статистики) ===
Собран и протестирован в Play финал игры. Вся геометрия — плейсхолдер/грейбокс, от моделей Никиты не зависит.

ПОТОК ФИНАЛА:
- Игрок ставит ПОСЛЕДНИЙ (9-й) артефакт на стенд в лобби → GameManager.OnAllArtifactsPlaced.
- В ЛОББИ запускается сиквенс (FinaleSequenceController): загорается кольцо-подсветка на карте (объект RingGlow,
  LineRenderer-круг, материал Art/Materials/RingGlow.mat) → через 1 с поднимается раздвижная дверь (DoorPanel едет
  вверх на 2.6 м за 2 с) в воротах FinaleGate (сзади от спавна, z=-4) → сиквенс «вооружается».
- Игрок ПОДХОДИТ к проёму: когда голова (Camera.main) входит в DoorwayZone (триггер-коллайдер) →
  SceneLoader.GoToFinale() → сцена Finale.
- Сцена Finale: экран статистики (FinaleStatsScreen) — время прохождения, число ошибок, 3 медали с подсветкой
  заработанной. Кнопки «Пройти заново» (SceneLoader.RestartToLobby) и «Выход» (Application.Quit / стоп Play в редакторе).

НОВОЕ В ЯДРЕ (правка НА МЕСТЕ, guid сохранён):
- GameManager: счётчик ошибок (свойство Mistakes, метод RegisterMistake(), событие OnMistakeRegistered) + таймер
  прохождения (NotifyCityEntered() стартует при ПЕРВОМ входе в город; стоп в момент OnAllArtifactsPlaced; свойство
  ElapsedSeconds). Ошибки и таймер сбрасываются в ResetProgress. DebugSetStatsForTesting(sec,mistakes) — только тест.
- SceneLoader: GoToFinale() + поле finaleSceneName (="Finale"); в GoToCity добавлен вызов GameManager.NotifyCityEntered().

ХУК ДЛЯ ВИКТОРИНЫ ТАНИ (важно): на КАЖДЫЙ неверный ответ вызывать  GameManager.Instance?.RegisterMistake();
  в точке, где викторина определяет «ответ неверный». Счётчик суммируется по всем городам и показывается в финале.

МЕДАЛИ: Assets/_Project/Art/UI/Medals/medal_{gold,silver,bronze}.png (импортированы как Sprite). Пороги по времени —
  поля FinaleStatsScreen (дефолт: золото ≤10 мин, серебро ≤15 мин, дольше — бронза, бронза гарантирована). Показаны все
  три, заработанная подсвечивается, остальные притушены.

НОВЫЕ СКРИПТЫ: Core/FinaleStatsScreen.cs, Core/FinaleSequenceController.cs; _Debug/DebugFinaleTrigger.cs (ТЕСТ).
СЦЕНА: Assets/_Project/Scenes/Core/Finale.unity (окружение + SpawnPoint + world-space экран). Build Settings: Finale
  добавлен (индекс 6). ВАЖНО: холст финала развёрнут лицом к игроку (rotation y=0; при y=180 текст был зеркальным).
ЛОББИ: добавлены RingGlow, FinaleGate (Pillar_L/R, Lintel, DoorPanel, DoorwayZone), FinaleSequence (контроллер),
  TEST_FinaleTrigger + world-space кнопка «ТЕСТ: Финал» (зовёт DebugFinaleTrigger.TriggerFinale).

ПРОВЕРЕНО В PLAY (0 ошибок, только haptic-варнинг симулятора): Bootstrap→лобби→«ТЕСТ: Финал» ставит все 9 →
  кольцо горит, дверь поднимается → подход головы к проёму → Finale: время 12:00, ошибок 3, серебро подсвечено →
  «Пройти заново» → сброс (placed 0, mistakes 0, elapsed 0) и возврат в лобби.

ЖИВОЙ ТЕСТ ЗА ЖЕНЕЙ (в шлеме): кнопкой «ТЕСТ: Финал» (или реально поставив артефакты) → увидеть кольцо и подъём
  двери → подойти к проёму СЗАДИ от спавна (z=-4) → экран финала → проверить обе кнопки.

ОСТАЛОСЬ/ХВОСТЫ ПО ФИНАЛУ:
- Заменить плейсхолдеры моделями Никиты: раздвижная дверь (DoorPanel/FinaleGate), красивое «кольцо» на карте.
- Вживить RegisterMistake() в викторину Тани (сейчас счётчик растёт только от теста).
- ПЕРЕД СДАЧЕЙ удалить/выключить: TEST_FinaleTrigger (+кнопка) и TEST_DebugUnlock в лобби.
- Звук замыкания кольца (поля sfx/ringClip на FinaleSequenceController есть, клип пока не назначен).
=== КОНЕЦ ОБНОВЛЕНИЯ 03.10.2026 (финал) ===


=== ОБНОВЛЕНИЕ 03.10.2026 (КАРТА-МАРШРУТ: пошаговое открытие, цвета, пути, золотое кольцо) ===
Карта в лобби переведена из «все маркеры + попап» в пошаговый маршрут по прогрессу. Фон карты — спутниковый
снимок Art/UI/map_lobby.png (без жёлтых пинов) на MapWall (куб сделан квадратным 3.4×3.4, материал
Art/Materials/LobbyMap.mat, URP/Unlit). Маркеры расставлены по географии (опора — Кострома).

ПОРЯДОК ГОРОДОВ ИЗМЕНЁН (Кострома — первая). CityData.order и порядок GameConfig.cities:
  0 Кострома, 1 Ярославль, 2 Ростов, 3 Переславль, 4 Сергиев Посад, 5 Москва, 6 Владимир, 7 Суздаль, 8 Иваново.
  Старт highestUnlocked=0 → Кострома открыта сразу. TEST_DebugUnlock для входа в Кострому больше НЕ нужен и ВЫКЛЮЧЕН.

ЛОГИКА КАРТЫ — Scripts/Interaction/CityRouteMap.cs (на объекте Map):
  - маркер ПРОЙДЕН (IsQuizCompleted) → зелёный, не кликабелен; ТЕКУЩИЙ (открыт, не пройден) → жёлтый, клик → сразу GoToCity; ЕЩЁ ЗАКРЫТ → скрыт;
  - под каждым видимым маркером подпись (TextMeshPro 3D, шрифт Arial SDF, rotation=identity — лицом к игроку);
  - серый путь (LineRenderer) между соседними городами появляется при открытии следующего;
  - когда пройдены ВСЕ — все пути становятся золотыми + добавляется замыкающий отрезок (Иваново→Кострома): кольцо замкнуто;
  - обновление на Start (лобби грузится заново при каждом возврате) и по событиям GameManager (OnQuizCompleted/OnCityUnlocked/OnProgressReset);
  - клик срабатывает только у текущего города и только если его сцена есть в Build Settings (Application.CanStreamedLevelBeLoaded).

КЛИК БЕЗ ПОПАПА: CityMarker упрощён (select → CityRouteMap.OnMarkerClicked → прямой GoToCity). Старый MapController и
объект «Map Popup» ОТКЛЮЧЕНЫ (Map Popup SetActive=false; скрипт MapController оставлен неиспользуемым).

ПРОВЕРЕНО В PLAY (0 ошибок, консоль чистая): старт — видна только жёлтая Кострома с подписью, остальные скрыты;
прошёл Кострому → Кострома зелёная, Ярославль жёлтый, серый путь между ними; прошёл все 9 → все зелёные,
дорога золотая, кольцо замкнуто (9 отрезков, включая замыкающий).

ЗАМЕТКИ/ОСТАЛОСЬ:
  - Реальный вход кликом работает только в города с готовой сценой в билде (сейчас Кострома; Ярославль — на старой схеме).
    Остальные откроются по мере сборки городов.
  - Подписи близких городов (Кострома/Ярославль, Ростов/Переславль) слегка налезают — при желании подвинуть маркеры/подписи.
  - Серые сферы-маркеры — плейсхолдер; можно заменить иконками-пинами.
  - ПЕРЕД СДАЧЕЙ: TEST_DebugUnlock уже выключен; не забыть выключить/удалить TEST_FinaleTrigger.
=== КОНЕЦ ОБНОВЛЕНИЯ 03.10.2026 (карта-маршрут) ===


=== УТОЧНЕНИЕ 03.10.2026 (подписи в сцене + тест-кнопка «+1 город») ===
- Подписи городов теперь ПОСТОЯННЫЕ объекты сцены: Map/Labels/Label_<id> (TextMeshPro 3D, видны в редакторе).
  CityRouteMap их НЕ создаёт, а находит по имени (Labels/Label_<id>) и только включает/выключает по видимости.
  Пути (LineRenderer) по-прежнему строятся в рантайме; при желании их тоже можно сделать объектами сцены.
- Добавлена ТЕСТ-кнопка «ТЕСТ: +1 город» в лобби (объекты Advance Test Canvas + TEST_AdvanceCity; скрипт
  Scripts/_Debug/DebugAdvanceCity.cs): каждый клик засчитывает викторину текущего города (CompleteQuiz) → открывается
  следующий. Для проверки карты. УДАЛИТЬ перед сдачей (как TEST_DebugUnlock и TEST_FinaleTrigger).
- ГРАБЛИ: world-space канвасы кнопок в лобби читаемы при euler Y≈329.7 (а не 140 — это обратная сторона, текст зеркалит).
- Проверено в Play: старт — только Кострома (маркер+подпись из сцены); «+1 город» → Кострома зелёная, Ярославль жёлтый,
  серый путь; подписи включаются по мере открытия. Консоль чистая.
=== КОНЕЦ УТОЧНЕНИЯ 03.10.2026 ===


=== ОБНОВЛЕНИЕ 05.10.2026 (ЯРОСЛАВЛЬ ЗАВЕРШЁН: маршрут EVE на EvaZoneGuide + артефакт + коллайдеры стен) ===
Ярославль переведён на боевую схему EvaZoneGuide по образцу Костромы и ЗАКОНЧЕН.

ЛОКАЦИЯ (сделал Женя): комната `room` (scale 0.3). Спавн снаружи `GuideModule/SpawnPoint`; телепорт-платформа `GuideModule/TeleportWaypoint` → `room/TeleportPoint` внутри (anchor/collider/слой в порядке). Рабочий набор точек/зон лежит под `room/` (ВКЛючён). Старый дубль под `GuideModule/EVEHelpObject` (EvaVoice*/EvaPoint2-7) ВЫКЛючен и НЕ используется. SpawnPoint-компонент в сцене один (GuideModule/SpawnPoint) — дубля нет.

EVE (инстанс в корне): `RobotGuide` и `NavMeshAgent` ВЫКЛючены, добавлен `EvaZoneGuide` (useNavMesh=false — движение по прямой, faceYawOffset=90). Ссылки: audioSource=EVE AudioSource, city=City_yaroslavl, screenUI=`EVE Screen Canvas` (выключен до финала), quizButton=`QuizButton` (onClick перевязан с RobotGuide.GoQuiz на EvaZoneGuide.GoQuiz).
СЦЕНАРИЙ (аудио Audio/Guide/Yaroslavl/Yaroslavl1..7.mp3):
- intro = Yaroslavl1: zone/evaPoint пустые → играет сразу на старте, EVE стоит СНАРУЖИ у входа (приветствие), потом телепортируется внутрь первым стопом.
- stops по порядку: EVApoint2 (teleportToPoint=TRUE — прыжок внутрь) /Zone2/Yaroslavl2 → EVApoint3/Zone3/Yaroslavl3 → EVApoint4/Zone4/Yaroslavl4 → EVApoint5/Zone5/Yaroslavl5 → EVApoint5.1 (НЕМОЙ вейпоинт: zone=null, clip=null) → EVApoint6/Zone6/Yaroslavl6.
- taskClip ПУСТОЙ; finaleClip = Yaroslavl7. Камень разблокируется сразу ПОСЛЕ фразы на EVApoint6 (последний стоп, SetArtifactGrabbable(true)); Yaroslavl7 играет уже ПОСЛЕ сбора артефакта, затем показывается кнопка викторины.
Зоны ZoneToActivateEva2..6 (isTrigger=true, рендер выкл) и точки EVApoint* (коллайдер/рендер выкл) были настроены по §7 — не трогал.

АРТЕФАКТ `room/ArtifactRock` настроен как в Костроме (§5): Rigidbody(isKinematic=true, useGravity=false), сохранён родной BoxCollider, XRGrabInteractable(interactionLayers=Default=1, throwOnDetach=false, enabled=FALSE — блок до рассказа; EvaZoneGuide включит после Yaroslavl6), ArtifactPickup(city=City_yaroslavl), StickyArtifact. Хват ПРАВОЙ рукой (схема Vive). Камень стоит среди декоративных SM_Rocks — игрок ищет нужный.

КОЛЛАЙДЕРЫ СТЕН (фикс «изнутри проходишь сквозь стены»): стены комнаты — меш `Cube.002` с ОДНОСТОРОННИМ MeshCollider (нормали наружу): снаружи войти нельзя, изнутри проходишь насквозь. Добавлен замкнутый контур из 4 простых BoxCollider — объект `RoomColliders_Yaroslavl` в КОРНЕ сцены (identity-трансформ, чтобы scale 0.3 комнаты не искажал размеры): север z=10.9, юг z=5.7, запад x=-7.75, восток x=10.95; высота y[-16.3..-12.9] (шелл Cube.002), толщина 0.3. Родной меш НЕ трогал — боксы поверх. Теперь непробиваемо в обе стороны (стик-перемещение + телепорт-луч); физ-ходьбу головой держит общий HeadCollisionPushback на PlayerRig.

ПРОВЕРЕНО: компиляция/консоль чистые (0 ошибок), сцена сохранена; поля EvaZoneGuide и компоненты ArtifactRock считаны обратно — всё на местах; рейкасты из центра комнаты наружу попадают в новые боксы.
ЖИВОЙ ТЕСТ В ШЛЕМЕ (за командой): полный проход Yaroslavl1 (снаружи) → телепорт внутрь → зоны 2–6 (EVE идёт по точкам, 5.1 молча) → после Yaroslavl6 взять камень правой рукой → Yaroslavl7 → кнопка «Пройти викторину»; упереться в каждую стену, прицелиться телепортом «сквозь стену» (луч должен блокироваться).
ХВОСТ: кнопка викторины ведёт в city.quizSceneName=Quiz_Yaroslavl — убедиться, что сцена есть в Build Settings (иначе переход не сработает). Свести вид артефакта лобби/викторина (Artifact_Yaroslavl) с комнатным ArtifactRock — общий хвост как у Костромы.

СТАТУС ГОРОДОВ: готовы КОСТРОМА, ЯРОСЛАВЛЬ и ИВАНОВО (Иваново ранее забыли отметить — фиксируем). Остальные — по мере сборки.
=== КОНЕЦ ОБНОВЛЕНИЯ 05.10.2026 ===

=== ОБНОВЛЕНИЕ 06.10.2026 (Суздаль-гид + артефакт, счётчик ошибок викторин, сцены Тани — боевые) ===

A) СУЗДАЛЬ (City_Suzdal) — гид и артефакт (модели Никиты ещё частично плейсхолдеры).
- EVE переведена со старого RobotGuide на EvaZoneGuide (RobotGuide.enabled=false, NavMeshAgent.enabled=false, добавлен EvaZoneGuide, useNavMesh=false, faceYawOffset=90). audioSource=EVE AudioSource, city=City_suzdal, screenUI='EVE Screen Canvas', quizButton=QuizButton (onClick перевязан с RobotGuide.GoQuiz на EvaZoneGuide.GoQuiz).
- Аудио Suzdal1..6 сопоставлены с зонами EVEHelpObject: intro=Suzdal1@EvaVoice1ActivateZone (EVE снаружи); stops: Suzdal2@EvaPoint2/EvaVoice2ActivateZone (teleportToPoint=true — прыжок внутрь), Suzdal3@EvaPoint3/Zone3, Suzdal4@EvaPoint4/Zone4, Suzdal5 (срубы+подсказка)@EvaPoint5/Zone5 — ПОСЛЕДНИЙ stop; taskClip=null → артефакт разблокируется сразу после Suzdal5; finaleClip=Suzdal6 (итог, играет после сбора). EvaPoint6/EvaVoice6ActivateZone и EvaPoint7 остались СВОБОДНЫМИ (Suzdal6 — пост-сбор финал, не зонный).
- Артефакт = модель 'brevno (07)' (единственный сруб с хватом; остальные брать нельзя). Настроено по §5: Rigidbody isKinematic=true/useGravity=false; XRGrabInteractable throwOnDetach=false, interactionLayers=Default(1), enabled=FALSE (блок до рассказа — EvaZoneGuide включит после Suzdal5); добавлены ArtifactPickup(city=City_suzdal) и StickyArtifact.
- ХВОСТЫ Суздаля: телепорт-вход (TeleportWaypoint + TeleportPoint) пока ВЫКЛючены — Жене включить для входа внутрь; меши зон/точек ещё видимы (скрыть по §7 перед сдачей); CityData.artifactPrefab суздаля пуст (хвост «свести вид артефакта лобби/викторина с комнатным», как у Костромы/Ярославля).

B) СЧЁТЧИК ОШИБОК ВИКТОРИН подключён к сценам Тани. Переменную НЕ создавали — она уже была (GameManager: _mistakes, свойство Mistakes, RegisterMistake(), событие OnMistakeRegistered, сброс в ResetProgress; FinaleStatsScreen уже показывает ошибки).
- Новый компонент Scripts/Quiz/QuizMistakeCounter.cs: в Awake добавляет РАНТАЙМ-слушатель на свою Button → GameManager.Instance?.RegisterMistake(). Существующий onClick кнопки не трогается (у falsee/Button это SetActive(false)). Awake у объекта, стартующего неактивным, срабатывает один раз → дубля подписки нет.
- Повешен на кнопку закрытия окна «неверно» quizbase/falsee/Button во ВСЕХ 9 сценах Тани (truee/Button = верный ответ → QuizBase.Next; falsee/Button = закрыть ошибку, остаться на вопросе → ровно 1 ошибка на 1 неверный ответ). В каждой сцене есть ВТОРОЙ неполный quizbase (1) (в Tanya_Kostroma — 'ok') с falsee БЕЗ кнопки — пропущен (см. задачи на завтра).

C) СЦЕНЫ ТАНИ СДЕЛАНЫ БОЕВЫМИ.
- CityData.quizSceneName ВСЕХ 9 городов переведены со стабов Quiz_<city> на Tanya_<city>: kostroma→Tanya_Kostroma, yaroslavl→Tanya_Yaroslavl, rostov→Tanya_Rostov, pereslavl→Tanya_Pereslavl, sergiev→Tanya_SergievPosad, moscow→Tanya_Moscow, vladimir→Tanya_Vladimir, suzdal→Tanya_Suzdal, ivanovo→Tanya_Ivanovo.
- Все 9 Tanya_<city> добавлены в Build Settings (enabled), индексы [9]..[17]. Старый стаб Quiz_Kostroma [4] остался включённым, но на него больше никто не ссылается (выключить/удалить — в задачах).
- ВНИМАНИЕ: сцены Тани ещё НЕ состыкованы с ядром (нет CompleteQuiz/возврата) и не проверены на лишний риг — см. ЗАДАЧИ НА ЗАВТРА ниже.
=== КОНЕЦ ОБНОВЛЕНИЯ 06.10.2026 ===


=== ЗАДАЧИ НА ЗАВТРА (запланировано 06.10.2026) ===
Контекст (сделано 06.10): сцены Тани сделаны боевыми — во ВСЕХ 9 CityData.quizSceneName прописаны Tanya_<city> (вместо старых Quiz_<city>) и все 9 Tanya_<city> добавлены в Build Settings (enabled). На кнопку закрытия окна «неверно» (quizbase/falsee/Button) во всех сценах Тани повешен новый компонент QuizMistakeCounter (Scripts/Quiz/QuizMistakeCounter.cs) → при закрытии окна ошибки зовёт GameManager.RegisterMistake(); счётчик ошибок (Mistakes) уже был готов и показывается на экране финала. Суздаль: EVE переведена на EvaZoneGuide (аудио Suzdal1→intro/зона1, Suzdal2-4→stops/зоны2-4, Suzdal5 (срубы+подсказка)→последний stop/зона5, Suzdal6→finaleClip после сбора), бревно 'brevno (07)' сделано артефактом (kinematic, grab выкл до рассказа, ArtifactPickup city=suzdal + StickyArtifact).

1) ВОЗВРАТ ИЗ ВИКТОРИНЫ — стыковка сцен Тани с ядром. СЕЙЧАС: Таня-QuizBase умеет только Next() (листать вопросы); НЕТ вызова GameManager.CompleteQuiz(city) и перехода назад → после прохождения город не открывается и игрок не возвращается в лобби. СДЕЛАТЬ: по успешному прохождению викторины звать GameManager.CompleteQuiz(city) + SceneLoader.GoToLobby(); по дизайну 29.09 — держатель артефакта (положил артефакт → запуск викторины; забрал → в руку); выходы 'назад в город' (доступен всегда, переслушать EVE) и 'в лобби' (только после прохождения). Образец логики — старый QuizStubController. ПОБОЧНО: старый стаб Quiz_Kostroma остался включённым в Build Settings, но на него больше никто не ссылается — выключить/удалить.

2) ПРОВЕРКА СТРУКТУРЫ СЦЕН ТАНИ под архитектуру. Контентная сцена должна содержать ТОЛЬКО окружение + SpawnPoint + интерактив, БЕЗ рига/Main Camera/EventSystem/менеджеров (они постоянные из Bootstrap). Проверить КАЖДУЮ Tanya_<city>: нет ли своего XR-рига/камеры/EventSystem (убрать — иначе задвоится с Bootstrap), есть ли SpawnPoint. ТАКЖЕ: в каждой сцене найден ВТОРОЙ неполный quizbase (1) (в Tanya_Kostroma — объект 'ok') с falsee БЕЗ кнопки закрытия — решить: мусор (удалить) или второй блок вопросов (добавить кнопку закрытия, тогда повесить на неё QuizMistakeCounter, сейчас она не считается).
=== КОНЕЦ ЗАДАЧ НА ЗАВТРА ===


=== ОБНОВЛЕНИЕ 07.10.2026 (СЕРГИЕВ ПОСАД: EvaZoneGuide + свап линзы-артефакта) ===
Город Сергиев Посад (City_SergievPosad, ЗОМЗ/Швабе — оптика) переведён на боевую схему.

ДИАЛОГИ EVE переписаны (6 фраз, текст — для озвучки позже, аудио пока НЕ подложено):
  1 вступление (о заводе, расширено), 2 история в приборах (без изменений), 3 как работает оптика (без изменений),
  4 ПЕРЕПИСАНА: про прицелы и бинокли (БЕЗ упоминания артефакта/«возьми линзу»),
  5 taskClip: подведение итогов + подсказка «линза скатилась со стенда и укатилась на пол — найди и подбери»,
  6 finaleClip: уровень пройден, последний артефакт кольца, зовёт на викторину.
  Полные тексты — в чате/контент-файле SergievPosad_content.md (точки 1-5 там устарели, см. новые 6 фраз).

СВАП ЛИНЗЫ (2 линзы в сцене):
  - room/stand hight (3)/Lens — ДЕКОРАТИВНАЯ линза на стенде (active, без компонентов). Это decoy.
  - LensArtifact (корень room, был INACTIVE) — НАСТОЯЩИЙ артефакт на полу. Настроен по §5:
    Rigidbody(kinematic,noGravity) + BoxCollider(подогнан к мешу) + XRGrabInteractable(layers=Default=1,
    throwOnDetach=false, enabled=FALSE — блок до показа) + ArtifactPickup(city=City_sergiev) + StickyArtifact.
    Стартово выключен.
  - НОВОЕ ПОЛЕ EvaZoneGuide.decoyToHideOnReveal (GameObject, опционально): в RevealArtifact() при показе
    артефакта (после taskClip=фраза 5) под затемнение ScreenFader прячет decoy. Итог: экран чернеет →
    линза со стенда исчезает → на полу появляется берущаяся линза → экран возвращается. Для других городов
    поле пустое — поведение не меняется.

EVE переведена с RobotGuide на EvaZoneGuide (RobotGuide.enabled=false, NavMeshAgent.enabled=false):
  useNavMesh=false, faceYawOffset=90, audioSource=EVE AudioSource, city=City_sergiev,
  screenUI='EVE Screen Canvas', quizButton=QuizButton (onClick перевязан RobotGuide.GoQuiz → EvaZoneGuide.GoQuiz).
  intro = фраза 1 (zone/evaPoint/clip пустые → играет сразу, EVE снаружи).
  stops: [0] История — EVApoint2/ZoneToActivateEva2, teleportToPoint=TRUE (прыжок внутрь);
          [1] Оптика — EVApoint3/ZoneToActivateEva3; [2] Прицелы и бинокли — EVApoint4/ZoneToActivateEva4.
  taskClip(фраза5)/finaleClip(фраза6) = null (аудио подложить позже). artifact=LensArtifact, decoy=стенд-линза.
  НЕ использованы (свободны): room/EVApoint5, EVApoint5.1, EVApoint6, ZoneToActivateEva5/6 и весь
  GuideModule/EVEHelpObject (старый набор). Если EVE должна подойти ближе к месту падения линзы перед
  фразой 5 — добавить немой вейпоинт-стоп (zone=null, clip=null, evaPoint=EVApoint5).

ПОЧИНЕНО (блокировало загрузку города):
  - City_SergievPosad НЕ был в Build Settings → добавлен и включён.
  - CityData.citySceneName была 'City_Sergiev' (опечатка, сцена называется City_SergievPosad) → исправлено.
  - CityData.artifactPrefab у Сергиева пуст (как у Костромы/Ярославля — лобби/викторина используют
    data-driven сферу Artifact_*, а в комнате модель). Общий хвост «свести вид артефакта».

ПРОВЕРЕНО: компиляция и консоль чистые (0 ошибок), сцена/CityData/BuildSettings сохранены.
ЖИВОЙ ТЕСТ ЗА КОМАНДОЙ (шлем): полный проход — intro снаружи → телепорт внутрь (стоп Eva2) → зоны 3,4
  (EVE идёт по прямой между EVApoint2→3→4; проверить, что путь свободен от стен, иначе добавить вейпоинты) →
  после фразы 5 (taskClip) экран чернеет, стенд-линза пропадает, напольная линза появляется → взять её ПРАВОЙ
  рукой → фраза 6 → кнопка «Пройти викторину» → Tanya_SergievPosad. Подложить аудио Sergiev1..6 в intro/stops/
  taskClip/finaleClip. Проверить позицию LensArtifact на полу (сейчас прежняя, Женя поправит в шлеме).
=== КОНЕЦ ОБНОВЛЕНИЯ 07.10.2026 (Сергиев Посад) ===


=== ОБНОВЛЕНИЕ 08.10.2026 (Сергиев Посад аудио + фиксы викторины Костромы по прогону) ===
АУДИО СЕРГИЕВА ПОСАДА подложено Еве (EvaZoneGuide на EVE, City_SergievPosad):
  Sergiev1→intro, Sergiev2/3/4→stops (История/Оптика/Прицелы и бинокли), Sergiev5→taskClip (подсказка,
  после неё свап линзы), Sergiev6→finaleClip. Файлы Assets/_Project/Audio/Guide/Sergiev/Sergiev1..6.mp3.

ПРОГОН КОСТРОМЫ — три бага в сцене викторины Tanya_Kostroma пофикшены:
  1) СПАВН В ПУСТОТУ: SpawnPoint стоял на y=0, а пол комнат (Plane/Plane (1)) на y≈3.6-3.7 → игрок падал
     ниже пола. SpawnPoint поднят на y=3.72 (пол комнаты теста), x/z и разворот прежние. SceneLoader.TeleportTo
     ставит корень рига в позицию SpawnPoint, поэтому точка спавна ДОЛЖНА лежать на полу.
  2) АРТЕФАКТ НЕ ПЕРЕНОСИЛСЯ: в сцене не было ArtifactSceneSpawner. Добавлен объект ArtifactSpawner
     (sceneCity пустой — это сцена викторины): ветка 'собран и не поставлен → в руку' пересоздаёт
     Artifact_Kostroma в держащей руке при входе. ХВОСТ: в руке сфера-плейсхолдер Artifact_Kostroma,
     а в комнате был метеорит Никиты — свести вид (общий хвост Костромы/Ярославля/Сергиева).
  3) КНОПКА 'ВЕРНУТЬСЯ В ЛОББИ' после головоломки: порядок сцены викторины = тест → интерактив (сборка
     Солнечной системы, Planet.cs) → лобби. По верному ответу Planet.Plc() включает панель ok/truee и она
     остаётся. Под ok/truee добавлена кнопка 'Вернуться в лобби' (TMP Arial SDF) → QuizReturn
     (QuizStubController, city=City_kostroma).CompleteAndReturn = GameManager.CompleteQuiz(kostroma) +
     SceneLoader.GoToLobby(). Кнопка появляется вместе с панелью 'верно' (т.е. после прохождения). В лобби
     игрок ставит артефакт на Stand_Kostroma (PlaceArtifact → финал).
  Проверено: компиляция и консоль чистые (0 ошибок), сцена сохранена.
  ЖИВОЙ ТЕСТ: пройти тест → собрать Солнечную систему верно → нажать 'Вернуться в лобби' → в лобби артефакт
  в руке, поставить на стенд; проверить, что спавн теперь на полу (не падение).
  ПРИМ.: остальные Tanya_<city> ещё НЕ состыкованы (нет возврата/спавнера/проверки спавна) — повторить этот
  рецепт на каждую сцену викторины по мере прогона.
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 ===


=== ОБНОВЛЕНИЕ 08.10.2026 (спавн: смещение камеры + метеорит вместо сферы) ===
1) ГОРИЗОНТАЛЬНОЕ СМЕЩЕНИЕ СПАВНА (игрок появлялся в стороне от SpawnPoint). Причина: PlayerRig.TeleportTo
   ставил КОРЕНЬ XR Origin на точку, но HMD смещён внутри play area → игрок (камера) оказывался сбоку.
   Фикс (PlayerRig.cs, правка на месте): после разворота по рысканью компенсируем горизонтальное смещение
   камеры от корня — t.position = target.position - camOffsetXZ (origin.Camera). Теперь на SpawnPoint встаёт
   именно игрок. Общий фикс для ВСЕХ городов/сцен. В симуляторе (камера над корнем) поведение прежнее.
   Если где-то всё равно нужно подвинуть — правим X/Z самого SpawnPoint.
2) АРТЕФАКТ КОСТРОМЫ: сфера-плейсхолдер заменена на модель метеорита. Artifact_Kostroma.prefab теперь
   mesh=meteor_HP_Cube + mat=meteor_Roughness (из Prefabs/Kostroma/meteor), uniform scale≈9.85 (размер в руке
   ~0.18 м), SphereCollider заменён на BoxCollider по мешу. Компоненты артефакта (Rigidbody/XRGrabInteractable/
   ArtifactPickup/StickyArtifact) сохранены. Теперь в руке/в лобби/в викторине виден метеорит, как в комнате —
   хвост 'свести вид артефакта' для Костромы закрыт.
Проверено: компиляция и консоль чистые (0 ошибок).
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 ===


=== ОБНОВЛЕНИЕ 08.10.2026 (Ярославль: сведение сцены викторины для полного прохождения) ===
Tanya_Yaroslavl состыкована с ядром по образцу Костромы:
  1) СПАВН: SpawnPoint был на y=0 (пол комнат ~y=3.6-3.7) → поднят на y=3.72 (пол комнаты теста), x/z прежние.
     Горизонтальный увод спавна лечит общий фикс PlayerRig.TeleportTo (ставит ИГРОКА/камеру на точку).
  2) ПЕРЕНОС АРТЕФАКТА: добавлен ArtifactSceneSpawner (sceneCity пустой) — собранный артефакт появляется в руке.
  3) КНОПКА 'ВЕРНУТЬСЯ В ЛОББИ': интерактив Ярославля = сейсмограф (Sismograph, 'поймать толчки'). По успеху
     Sismograph.OnAllFixed включает панель quizbase (1)/truee. Под неё добавлена кнопка 'Вернуться в лобби'
     (TMP Arial SDF) → QuizReturn (QuizStubController, city=City_yaroslavl).CompleteAndReturn = CompleteQuiz +
     GoToLobby. Появляется вместе с панелью 'верно' (после прохождения). Порядок: тест → сейсмограф → лобби.
  4) АРТЕФАКТ: Artifact_Yaroslavl.prefab переведён с куба-плейсхолдера на модель камня из комнаты —
     mesh=SM_Rocks_06 + mat='Rocks Stylized_M' (PolyOne), scale=0.5 (как ArtifactRock в комнате, ~0.18 м),
     BoxCollider по мешу. Компоненты артефакта сохранены. Вид в руке/лобби/викторине совпал с комнатным камнем.
Проверено: компиляция и консоль чистые (0 ошибок); кнопка onClick, спавнер и SpawnPoint.y подтверждены чтением.
ГОТОВЫ К ПОЛНОМУ ПРОХОЖДЕНИЮ (цикл город→викторина→лобби): Кострома, Ярославль. Остальные Tanya_<city> —
повторить рецепт (SpawnPoint на пол + ArtifactSceneSpawner + кнопка 'Вернуться в лобби' на success-панель +
свести вид артефакта) по мере прогона.
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 (Ярославль) ===


=== ОБНОВЛЕНИЕ 08.10.2026 (вечер: артефакт не занимает руку в викторине + корректная высота спавна) ===
ПРОБЛЕМА (Таня): взял метеорит → рука занята навсегда, в викторине нельзя брать объекты (планеты и т.п.).
Причина: ArtifactSceneSpawner в сцене викторины форс-вкладывал собранный артефакт В РУКУ, а StickyArtifact
не давал его выпустить (возврат в руку при любом отпускании не в сокет стенда) → держащая рука заблокирована.
ФИКС: ArtifactSceneSpawner получил режим викторины — новые поля parkCollectedHere(bool)+parkAnchor(Transform).
  Когда parkCollectedHere=true: собранный артефакт НЕ идёт в руку, а ставится на parkAnchor, делается kinematic,
  а StickyArtifact на нём ОТКЛЮЧАЕТСЯ. Итог: в викторину входишь с пустыми руками (артефакт стоит рядом,
  виден — «перенёсся»), обе руки свободны для головоломки; взять/положить его можно свободно (без приклеивания).
  В лобби/городе поведение прежнее (sticky работает, несём на стенд) — там parkCollectedHere=false.
  Включено в Tanya_Kostroma и Tanya_Yaroslavl (объект ArtifactParkAnchor у точки спавна).

ПОПУТНО ИСПРАВЛЕНА ВЫСОТА СПАВНА в сценах викторин (моя прошлая ошибка): я ставил SpawnPoint на y=3.72,
но это ПОТОЛОК/бэкдроп (Plane), а реальный ходибельный пол — это Cube на y≈0.13 (проверено рейкастом вниз).
SpawnPoint обеих викторин опущен на y=0.18 (пол+0.05). Теперь игрок стоит на полу, а не на потолке.
(Городских сцен это НЕ касалось — там спавн/телепорт настраивала команда в шлеме, я их не трогал.)
Проверено: компиляция и консоль чистые; поля спавнеров и SpawnPoint.y подтверждены чтением.
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 (вечер) ===


=== ОБНОВЛЕНИЕ 08.10.2026 (лобби: новые оформленные подиумы приняли роль стендов) ===
В лобби появился красивый набор из 9 подиумов (объекты Tanya/Podium<City>). У каждого уже был дочерний
узел 'Socket' со SphereCollider, но без логики. Довёл их до рабочих стендов (как Stand_Kostroma/Yaroslavl):
  на каждый Socket добавлены XRSocketInteractor (interactionLayers=Everything, socketActive=true) +
  StandSocket с назначенным городом; коллайдер сделан триггером, радиус поднят до ~0.15 world (как у рабочего
  стенда, иначе ловил бы артефакт только вплотную). Соответствие подиум→город:
  PodiumKostorma→kostroma, PodiumYaroslavl→yaroslavl, PodiumSuzdal→suzdal, PodiumRostov→rostov,
  PodiumPereslavl→pereslavl, PodiumSergiev→sergiev, PodiumMoscow→moscow, PodiumVladimir→vladimir,
  PodiumIvanovo→ivanovo. Каждый принимает ТОЛЬКО свой артефакт (StandSocket фильтрует по artifactId) и при
  установке зовёт GameManager.PlaceArtifact (финал ловится сам). Socket стоит на ~0.08 выше верха подиума —
  артефакт ложится на поверхность.
  Старые плейсхолдер-стенды Stand_Kostroma и Stand_Yaroslavl ВЫКЛЮЧЕНЫ (SetActive=false, обратимо), чтобы не
  было двух сокетов на один город (ArtifactSceneSpawner берёт активные сокеты). Итог: ровно 9 активных стендов,
  по одному на город.
  ПРИМ.: artifactPrefab задан пока только у kostroma/yaroslavl — у остальных подиумов сокет готов, но пока
  нечего в них ставить, пока не назначены префабы артефактов тех городов (и не пройдены их викторины).
Проверено: консоль чистая; 9 активных StandSocket (по городу), 2 старых — неактивны. Сцена сохранена.
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 (подиумы лобби) ===


=== ОБНОВЛЕНИЕ 08.10.2026 (Кострома: держатель артефакта в викторине + открытие города постановкой) ===
Переделан поток Костромы под дизайн 29.09 (держатель в сцене викторины). Отменён прежний 'park' в руках.

НОВЫЙ ПОТОК КОСТРОМЫ:
  1) Город→викторина: артефакт по-прежнему ПРИКЛЕЕН к руке (ArtifactSceneSpawner.parkCollectedHere=false,
     StickyArtifact работает — донести можно только до держателя).
  2) В сцене Tanya_Kostroma добавлен ДЕРЖАТЕЛЬ (объект QuizHolder: пьедестал-цилиндр + узел Socket с
     XRSocketInteractor + новым компонентом QuizArtifactHolder, city=kostroma). Игрок кладёт артефакт в него —
     руки освобождаются для головоломки.
  3) Пока интерактивная викторина НЕ пройдена — артефакт нельзя забрать: держатель возвращает его в гнездо.
     Разблокировка — когда панель 'верно' (Planet.truee) становится активной (completionIndicator).
  4) После разблокировки игрок забирает артефакт → QuizArtifactHolder делает SceneLoader.GoToLobby().
  5) В ЛОББИ: постановка артефакта на подиум теперь ОТКРЫВАЕТ следующий город — StandSocket.OnSelectEntered
     кроме PlaceArtifact зовёт CompleteQuiz(city) (идемпотентно). Старая кнопка 'Вернуться в лобби' в сцене
     викторины и объект QuizReturn УДАЛЕНЫ (возврат теперь по забору артефакта из держателя).

НОВЫЙ СКРИПТ: Scripts/Interaction/QuizArtifactHolder.cs (сокет-держатель викторины: фильтр по artifactId,
  блокировка до прохождения, разблокировка по индикатору, забор → в лобби; StickyArtifact на артефакте
  отключается при попадании в держатель). ИЗМЕНЁН: StandSocket.cs (+CompleteQuiz при постановке).

ПОЛОЖЕНИЕ ДЕРЖАТЕЛЯ: QuizHolder в интерактивной комнате (1.3, 0.24, 4.5), сокет на ~1.14 — ПЛЕЙСХОЛДЕР,
  Женя/Таня подвинут куда удобно в шлеме (цилиндр-пьедестал — временный визуал, заменить красивым стендом).

ХВОСТ: Ярославль пока на СТАРОЙ схеме (park + кнопка 'Вернуться в лобби', которая сама зовёт CompleteQuiz).
  Работает (CompleteQuiz идемпотентен, подиум тоже откроет город), но для единообразия Ярославль надо
  перевести на тот же держатель-поток (QuizArtifactHolder + убрать park/кнопку). Сделать по запросу.
Проверено: компиляция и консоль чистые; QuizArtifactHolder(city=kostroma, indicator=truee) и spawner
  (park off) подтверждены чтением.
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 (держатель Костромы) ===


=== ФИКС 08.10.2026 (клон артефакта в лобби при постановке) ===
СИМПТОМ: поставил артефакт на подиум в лобби — он 'клонировался' и не пропал из руки (две модели).
ПРИЧИНА: в лобби оказалось ДВА ArtifactSceneSpawner (второй — Tanya/ArtifactSpawner (1), приехал с набором
подиумов). Каждый на старте спавнил собранный артефакт в руку → два экземпляра: один ставишь на подиум,
второй остаётся в руке.
ФИКС: удалил дубль-спавнер (остался один ArtifactSpawner). Плюс ЗАКАЛИЛ ArtifactSceneSpawner: в начале
SpawnRoutine он теперь уничтожает все существующие ArtifactPickup в сцене перед спавном (единственный
источник правды — состояние GameManager). Это убирает не только дубли-спавнеры, но и артефакт, случайно
переживший смену сцены в руке.
Проверено: в лобби остался 1 спавнер; компиляция и консоль чистые.
=== КОНЕЦ ФИКСА 08.10.2026 ===


=== ОБНОВЛЕНИЕ 08.10.2026 (ВЛАДИМИР собран = ЭТАЛОННЫЙ ШАБЛОН связки город↔квиз↔лобби) ===
Владимир подключён целиком по образцу Костромы и проверен — теперь это ШАБЛОН для остальных городов.
Готовы по полному циклу: Кострома, Владимир (Ярославль — на старой park-схеме, переведём позже).

ЧТО СДЕЛАНО ДЛЯ ВЛАДИМИРА:
  - Artifact_Vladimir.prefab создан из модели шлема (Helmet, astronaut-helmet) — grab включён, компоненты
    артефакта (Rigidbody kinematic, BoxCollider, XRGrabInteractable throwOnDetach=false layers=Default,
    ArtifactPickup city=vladimir, StickyArtifact). CityData.artifactPrefab назначен.
  - City_Vladimir уже был собран командой (EVE на EvaZoneGuide, артефакт Helmet, 4 стопа) — не трогал.
  - Tanya_Vladimir: SpawnPoint опущен на пол (y=0.18), добавлен ArtifactSceneSpawner (в руку),
    добавлен QuizHolder (пьедестал+Socket+QuizArtifactHolder, city=vladimir, completionIndicator=quizbase (1)/truee).
  - Lobby PodiumVladimir уже принимал артефакт (StandSocket city=vladimir).
  - Build Settings: City_Vladimir добавлен, Tanya_Vladimir уже был.

======================= ШАБЛОН: КАК ПОДКЛЮЧИТЬ ГОРОД <City> =======================
Образцы: Кострома, Владимир. Пошагово для нового города:
1) CityData (City_<id>.asset): citySceneName=City_<City>, quizSceneName=Tanya_<City>, artifactId,
   artifactDisplayName, artifactPrefab (см. п.2). order уже задан.
2) Artifact_<City>.prefab (Prefabs/Artifacts/): взять модель артефакта города (как в комнате) + компоненты:
   Rigidbody(isKinematic=true,useGravity=false), Collider по мешу, XRGrabInteractable(throwOnDetach=false,
   interactionLayers=Default(1), ВКЛючён), ArtifactPickup(city=<id>), StickyArtifact. Назначить в CityData.artifactPrefab.
   (Проще всего: дублировать артефакт из City-сцены, включить grab, SaveAsPrefabAsset.)
3) City_<City>.unity: EVE на EvaZoneGuide (RobotGuide+NavMeshAgent off), артефакт-модель с ArtifactPickup(city)
   +StickyArtifact+XRGrabInteractable (grab блокируется до рассказа), кнопка викторины → EvaZoneGuide.GoQuiz
   (ведёт в city.quizSceneName). Это строит команда в шлеме (см. §12 CITY_RECIPE).
4) Tanya_<City>.unity (сцена викторины):
   a. SpawnPoint — поставить на ПОЛ: рейкастом вниз найти Cube-пол (обычно y≈0.13) и задать y=пол+0.05.
      ВНИМАНИЕ: Plane на y≈3.7 — это потолок/бэкдроп, НЕ пол.
   b. Ровно ОДИН ArtifactSceneSpawner (пустые sceneCity/parkCollectedHere) — артефакт приедет в руку.
      НЕ дублировать спавнер (два = клон артефакта).
   c. QuizHolder = пьедестал (визуал) + дочерний Socket: SphereCollider(isTrigger, r≈0.15) +
      XRSocketInteractor(interactionLayers=Everything/-1, socketActive=true) + QuizArtifactHolder
      (city=<id>, completionIndicator = панель 'верно' интерактивной викторины). Поставить в интерактивной комнате.
      completionIndicator: у Planet-викторин — Planet.truee (обычно quizbase (1)/truee); у сейсмографа —
      объект, который включает Sismograph.OnAllFixed.
   d. Кнопку 'Вернуться в лобби' НЕ добавлять (возврат — по забору артефакта из держателя).
5) Lobby: Podium<City>/Socket = XRSocketInteractor(layers -1) + StandSocket(city=<id>). Постановка на подиум
   вызывает PlaceArtifact + CompleteQuiz (открывает следующий город) — уже в коде StandSocket.
6) Build Settings: включить City_<City> и Tanya_<City>.
ПОТОК ИГРОКА: город (взять артефакт→рука; EVE→викторина) → квиз (положить в держатель→пройти→забрать→лобби)
  → лобби (поставить на подиум→откроется следующий город).
КЛЮЧЕВЫЕ СКРИПТЫ: ArtifactSceneSpawner, QuizArtifactHolder, StandSocket, StickyArtifact, ArtifactPickup.
==================================================================================
Проверено: компиляция и консоль чистые; весь чейн Владимира (CityData/Artifact/Tanya/Podium/Build) подтверждён.
=== КОНЕЦ ОБНОВЛЕНИЯ 08.10.2026 (Владимир/шаблон) ===


=== ФИКСЫ 09.10.2026 (Владимир: порядок табличек + блокировка артефакта в держателе) ===
1) ТАБЛИЧКИ (Planet) не проходились: блейд Checkly едет по X справа налево (с +9 к -7) и собирает таблички
   в порядке убывания X, а список правильного ответа Planets был задан в ОБРАТНОМ порядке → никогда не совпадал.
   Фикс: список Planets у Checkly в Tanya_Vladimir РАЗВЁРНУТ (теперь Kassio,Kassio,Swan,Swan,LiilBeat,LiilBeat,
   Cefe,Cefe,Dogs,Dogs,BigBear,BigBear — совпадает с направлением проезда). Сохранено в сцене.
   (Каждая табличка числится дважды — у неё два коллайдера, оба дают OnTriggerEnter; это штатно, список из 12 корректен.)
2) ДЕРЖАТЕЛЬ не блокировал артефакт: прежняя схема (ре-грэб назад в сокет) не срабатывала надёжно —
   артефакт можно было забрать до прохождения. ПЕРЕПИСАН QuizArtifactHolder на ДЕТЕРМИНИРОВАННУЮ блокировку
   по interaction-слою: при попадании в держатель артефакту ставится слой (1<<20), которого НЕТ у рук игрока
   (Default+Map) — сокет (Everything) держит, а рука взять не может. По прохождении (completionIndicator=панель
   'верно' активна) артефакту возвращается обычный слой (Default) — игрок забирает его, и забор из держателя
   вызывает SceneLoader.GoToLobby(). Правка общая → применяется и к Костроме (тот же компонент).
ПОТОК (подтверждён для Владимира): город → квиз (положить артефакт в держатель = заблокирован; пройти таблички;
   панель 'верно' разблокирует; забрать артефакт → лобби) → лобби (поставить на подиум → откроется следующий город).
ЗАМЕТКА ДЛЯ ШАБЛОНА: у Planet-викторин проверяй, что порядок Planets совпадает с направлением проезда Checkly
   (иначе «правильно» не засчитается). QuizArtifactHolder блокировкой по слою — часть шаблона, доп. настройки не требует.
Проверено: компиляция и консоль чистые; Planets развёрнут и сохранён; держатель Владимира socket=Everything.
=== КОНЕЦ ФИКСОВ 09.10.2026 ===


=== ОБНОВЛЕНИЕ 09.10.2026 (СУЗДАЛЬ: связка город<->квиз<->лобби по шаблону + артефакт прячется до 5 фразы) ===
Суздаль подключён по эталонному шаблону (Владимир/§17 CITY_RECIPE). Готов к полному циклу город->викторина->лобби.

АРТЕФАКТ ПРЯЧЕТСЯ ДО РАССКАЗА (City_Suzdal): объект brevnoArtifact (Plane/brevnoArtifact) выставлен SetActive(false)
  в сцене. EvaZoneGuide.RevealArtifact() сам включает его ПОСЛЕ последнего стопа (Suzdal5, 'Срубы+подсказка';
  taskClip=null, поэтому показ идёт сразу после 5-й фразы) — под затемнением ScreenFader: экран чернеет ->
  brevnoArtifact.SetActive(true)+grab включается -> экран возвращается. Компоненты артефакта уже были по §5
  (Rigidbody kinematic, BoxCollider, XRGrabInteractable throwOnDetach=false layers=Default enabled=false до показа,
  ArtifactPickup city=suzdal, StickyArtifact). ArtifactSceneSpawner в City_Suzdal нет (физ-артефакт — не спавним).

ПРЕФАБ АРТЕФАКТА: создан Prefabs/Artifacts/Artifact_Suzdal.prefab из brevnoArtifact (grab ВКЛючён, остальные
  компоненты сохранены). CityData City_suzdal.artifactPrefab назначен = Artifact_Suzdal (раньше был пуст).
  CityData суздаля: citySceneName=City_Suzdal, quizSceneName=Tanya_Suzdal, artifactId=suzdal_log,
  artifactDisplayName='Венец сруба'. Хвост 'свести вид лобби/викторина с комнатным' закрыт (один меш brevno).

СЦЕНА ВИКТОРИНЫ Tanya_Suzdal (по §17.3):
  - SpawnPoint поднят на пол: был y=0, пол (Cube) под спавном y=0.125 -> SpawnPoint.y=0.175 (x/z=0,-3.5).
  - Добавлен ОДИН ArtifactSceneSpawner (ArtifactSpawner, sceneCity/park пустые) -> собранный артефакт приедет в руку.
  - Добавлен QuizHolder (-2.2, 0.24, 6.5) рядом с зоной сборки сруба: пьедестал-цилиндр (визуал, плейсхолдер) +
    узел Socket: SphereCollider(isTrigger, r=0.15) + XRSocketInteractor(interactionLayers=Everything/-1, socketActive)
    + QuizArtifactHolder(city=suzdal, completionIndicator=quizbase (1)/truee).
  МЕХАНИКА СБОРКИ (Loga): 17 слотов Loga (Cube (9)/GameObject*) по тегу 'Spoon'; последний слот тег 'Roof'
    (GameObject (16)) по установке крыши активирует quizbase (1)/truee (+roof(1)) — это и есть индикатор прохождения.
    Сначала тест (quizbase, 6 вопросов), затем сборка сруба -> truee -> держатель разблокирует артефакт.
  Сцена чистая: своего рига/камеры/EventSystem НЕТ (берутся из Bootstrap).

BUILD SETTINGS: City_Suzdal НЕ был в билде (вход в город сломался бы) — ДОБАВЛЕН и включён ([20]).
  Tanya_Suzdal уже был включён ([16]).

ПОТОК: City_Suzdal (EVE 1-5 фраз -> после Suzdal5 показывается артефакт -> взять ПРАВОЙ рукой -> Suzdal6 ->
  кнопка 'Пройти викторину') -> Tanya_Suzdal (артефакт в руке -> положить в QuizHolder = заблокирован; пройти тест
  и собрать сруб; truee разблокирует; забрать артефакт -> лобби) -> лобби (PodiumSuzdal -> PlaceArtifact+CompleteQuiz).
Проверено: компиляция и консоль чистые (0 ошибок/варнингов после clear+refresh); spawner/holder/spawnpoint считаны обратно.
ЖИВОЙ ТЕСТ ЗА КОМАНДОЙ (шлем): полный проход Суздаля; проверить позицию QuizHolder (плейсхолдер — подвинуть куда удобно).
ХВОСТ: аудио EVE Suzdal1..6 уже подложены; меши зон/точек в City_Suzdal скрыть по §7; телепорт-вход City_Suzdal
  (TeleportWaypoint/TeleportPoint) — Жене включить (был выключен). Остальные города — по тому же шаблону.
=== КОНЕЦ ОБНОВЛЕНИЯ 09.10.2026 (Суздаль) ===


=== ОБНОВЛЕНИЕ 09.10.2026 (СУЗДАЛЬ: квиз Тани подключён + серия фиксов артефакта/двери/интро) ===
Суздаль доведён до полного прохождения город->викторина->лобби. Готовы по полному циклу: Кострома, Владимир, Суздаль
(Ярославль — на старой схеме). Всё проверено в Play/шлеме, консоль чистая.

1) СВЯЗКА Tanya_Suzdal С ЯДРОМ (по шаблону §17 CITY_RECIPE):
   - Создан Prefabs/Artifacts/Artifact_Suzdal.prefab из модели бревна; CityData City_suzdal.artifactPrefab назначен
     (artifactId=suzdal_log, citySceneName=City_Suzdal, quizSceneName=Tanya_Suzdal).
   - Tanya_Suzdal: SpawnPoint поднят на пол (y 0 -> 0.175; пол Cube y=0.125); добавлен ОДИН ArtifactSceneSpawner
     (sceneCity/park пустые -> артефакт в руку); добавлен QuizHolder (пьедестал-цилиндр + Socket: SphereCollider trigger r=0.15
     + XRSocketInteractor Everything + QuizArtifactHolder city=suzdal). completionIndicator = quizbase (1)/truee.
     ВАЖНО про механику сруба: сборка = 17 слотов Loga (Cube (9)/GameObject*) по тегу 'Spoon'; ПОСЛЕДНИЙ слот тег 'Roof'
     (GameObject (16)) по установке крыши активирует quizbase (1)/truee -> это и есть индикатор прохождения (разблокирует держатель).
   - Build Settings: City_Suzdal НЕ был в билде (вход в город сломался бы) — ДОБАВЛЕН; Tanya_Suzdal уже был.

2) АРТЕФАКТ ПРЯЧЕТСЯ ДО 5-Й ФРАЗЫ: brevnoArtifact в City_Suzdal выставлен SetActive(false). EvaZoneGuide.RevealArtifact()
   сам включает его после последнего стопа (Suzdal5; taskClip=null -> показ сразу после 5-й фразы) под затемнением ScreenFader.

3) ИНТРО НЕ ЗАПУСКАЛОСЬ: intro.zone в City_Suzdal был EvaVoice1ActivateZone (далеко от точки спавна), игрок в неё не заходил.
   Убрал intro.zone (null) — Suzdal1 играет сразу при спавне (как в Ярославле/Сергиеве). EVE встречает со стартовой позиции.

4) ОШИБКИ PhysX ПРИ ВЗЯТИИ (concave + dynamic / ClosestPoint / computeMassAndInertia): у всех брёвен и артефакта были
   лишние ДОЧЕРНИЕ вогнутые MeshCollider на 'Plane'/'Plane (1)' (мусор импорта). XRGrabInteractable.movementType=VelocityTracking
   делает тело динамическим при захвате -> PhysX падает на вогнутом коллайдере. ФИКС: удалил вогнутые дочерние MeshCollider у
   всех грэбабельных тел, оставил корневой BoxCollider: City_Suzdal 44 шт (22 тела), Tanya_Suzdal 34 шт (18 тел), + в префабе.
   Сборка Loga не затронута (ловит по тегу на корне, дочерние были Untagged).

5) АРТЕФАКТ БЫЛ ПЛОСКИМ В КВИЗЕ: префаб хранил только ЛОКАЛЬНЫЙ масштаб (1.64,7.78,0.78), а в городе бревно под родителем
   Plane(20,2,20) -> мировой масштаб (32.82,15.56,15.56). Запёк мировой масштаб в root localScale префаба — бревно нормального вида,
   коллайдер ~0.68x0.10x0.11 м. (Правило: артефакт-префаб из модели под масштабированным родителем — пекём lossyScale в root.)

6) ЧЁРНАЯ ДВЕРЬ НЕ ОТКРЫВАЛАСЬ ПОСЛЕ ТЕСТА: у кнопки 'продолжить' на панели quizbase/CloseQuiz ('...справился с 1 частью')
   3-й вызов onClick = Animator.Play('Close') с ПУСТОЙ целью (null). Привязал цель к Animator двери (объект 'Cube').
   NB: клип называется 'Close', но это анимация ОТКРЫТИЯ (стена уезжает z 2.4 -> -14.4 и вбок по X).

7) АРТЕФАКТ НЕ БРАЛСЯ ЛУЧОМ ПОСЛЕ РАЗБЛОКИРОВКИ: у BoxCollider артефакта в рантайме включался isTrigger (в edit-режиме и в
   префабе — выключен; ни наш код, ни боевой код XRI его не ставят — источник не локализован). Дальний луч (NearFar) ИГНОРИРУЕТ
   триггеры -> не наводится -> не взять. ФИКС (код, общий для всех городов): в QuizArtifactHolder добавлен метод ForceCollidersSolid
   — пока артефакт в держателе, его не-мешевые коллайдеры принудительно держатся не-триггерами (вызов в OnSelectEntered и в Update).
   Проверено в Play: после сборки сруба бревно берётся правой рукой и уходит в лобби.

СТАТУС: полный цикл Суздаля работает. Остальные города (Ростов, Переславль, Сергиев уже частично, Москва, Иваново) —
по тому же шаблону §17. Напоминания: правой рукой берут артефакт (левый = локомоция, слой Map); перед сдачей убрать TEST_* из лобби.
=== КОНЕЦ ОБНОВЛЕНИЯ 09.10.2026 (Суздаль) ===


=== ОБНОВЛЕНИЕ 09.10.2026 (ИВАНОВО: связка город<->квиз<->лобби по шаблону §17) ===
Иваново подключено по эталонному шаблону (Владимир/Суздаль). Город и артефакт уже были собраны командой —
дособрана плумбинг-часть (префаб артефакта, CityData.artifactPrefab, сцена викторины). Готовы по полному
циклу: Кострома, Владимир, Суздаль, Ярославль(стар.схема), Иваново.

1) CityData City_ivanovo: citySceneName=City_Ivanovo, quizSceneName=Tanya_Ivanovo, artifactId=ivanovo_crystal,
   artifactDisplayName=Кристалл — уже были; НАЗНАЧЕН artifactPrefab=Artifact_Ivanovo (был пуст). order=8 (последний город).
2) Artifact_Ivanovo.prefab СОЗДАН из комнатного CrystalAtrifact (City_Ivanovo): Rigidbody(kinematic,noGravity),
   SphereCollider(не-триггер), XRGrabInteractable(layers=Default=1, throwOnDetach=false, grab ВКЛ, movementType=Instantaneous),
   ArtifactPickup(city=ivanovo), StickyArtifact. Масштаб 15 = мировой (родитель RoomIvanovo единичный) — искажения нет,
   вогнутых дочерних коллайдеров нет (грабли Суздаля не применимы).
3) City_Ivanovo (сделано командой, НЕ трогал): EVE на EvaZoneGuide (RobotGuide+NavMeshAgent off, useNavMesh=false,
   faceYawOffset=90, city=ivanovo, finaleClip=Ivanovo6, 4 стопа), артефакт CrystalAtrifact (SetActive=false, EVE
   показывает после рассказа), кнопка QuizButton.onClick -> EvaZoneGuide.GoQuiz (-> city.quizSceneName=Tanya_Ivanovo).
4) Tanya_Ivanovo (сцена викторины, §17.3):
   - SpawnPoint поднят на пол: y 0 -> 0.175 (пол Cube под спавном y=0.125; x=0,z=-3.5). ВНИМАНИЕ: Plane на y~3.7 — потолок.
   - Добавлен ОДИН ArtifactSceneSpawner (объект ArtifactSpawner; sceneCity/park пустые -> собранный артефакт в руку).
   - Добавлен QuizHolder (скопирован из эталонного Tanya_Vladimir): Pedestal(цилиндр-визуал)+Socket
     (SphereCollider trigger r=0.15 + XRSocketInteractor layers=Everything/-1, socketActive + QuizArtifactHolder
     city=ivanovo, completionIndicator=quizbase (1)/truee). Позиция (-2.0,0.24,7.0) — ПЛЕЙСХОЛДЕР, подвинуть в шлеме.
   Механика викторины Иваново = тест(quizbase) -> интерактив 'шаги эксперимента' (вода/сахар, EventsWater/step),
   индикатор прохождения = quizbase (1)/truee. Сцена чистая (нет своего рига/камеры/EventSystem).
5) Лобби PodiumIvanovo/Socket: StandSocket(city=ivanovo)+XRSocketInteractor(layers=-1) — уже был настроен (08.10), проверен.
6) Build Settings: City_Ivanovo [7] и Tanya_Ivanovo [17] уже включены.

ПОТОК: City_Ivanovo (EVE -> после рассказа появляется кристалл -> взять ПРАВОЙ рукой -> Ivanovo6 -> кнопка 'Пройти
  викторину') -> Tanya_Ivanovo (артефакт в руке -> положить в QuizHolder=заблокирован; тест+эксперимент; truee
  разблокирует; забрать артефакт -> лобби) -> лобби (PodiumIvanovo -> PlaceArtifact+CompleteQuiz).
Проверено: компиляция/консоль чистые (только старые варнинги DoorPan аниматора финала, не связаны); поля префаба,
  CityData, спавнера, держателя, сокета и подиума считаны обратно — всё на местах.
ЖИВОЙ ТЕСТ ЗА КОМАНДОЙ (шлем): полный проход Иваново; подвинуть QuizHolder куда удобно; проверить, что дверь
  тест->интерактив (Animator на 'Cube') открывается и что quizbase (1)/truee включается по завершении эксперимента.
=== КОНЕЦ ОБНОВЛЕНИЯ 09.10.2026 (Иваново) ===

>>> ИВАНОВО: живой тест в шлеме ПРОЙДЕН (09.10.2026) — полный цикл город->викторина->лобби работает. Город ГОТОВ.
    Готовы по полному циклу: Кострома, Владимир, Суздаль, Ярославль(стар.схема), Иваново. Следующий: перевод Ярославля на держатель-схему.


=== ОБНОВЛЕНИЕ 09.10.2026 (ЯРОСЛАВЛЬ переведён со старой park-схемы на держатель §17) ===
Ярославль переведён на единый держатель-поток (как Кострома/Владимир/Суздаль/Иваново). Теперь ВСЕ эти 5 городов — по одной схеме.

Что изменено в Tanya_Yaroslavl:
- ArtifactSceneSpawner (ArtifactSpawner): parkCollectedHere True->FALSE, parkAnchor очищен (null). Теперь собранный
  артефакт приезжает В РУКУ (а не паркуется у спавна). sceneCity пуст.
- УДАЛЕНЫ объекты старой схемы: объект QuizReturn (QuizStubController) и кнопка quizbase (1)/truee/ReturnToLobbyButton.
  Возврат в лобби теперь — по ЗАБОРУ артефакта из держателя (как в шаблоне).
- ДОБАВЛЕН QuizHolder (скопирован из эталонного Tanya_Vladimir): Pedestal+Socket (SphereCollider trigger r=0.15 +
  XRSocketInteractor layers=Everything/-1, socketActive + QuizArtifactHolder city=yaroslavl,
  completionIndicator=quizbase (1)/truee — панель, которую включает Sismograph.OnAllFixed). Позиция (-1.8,0.24,7.3)
  рядом с сейсмографом (Game @ ~1.33,1.53,7.3) — ПЛЕЙСХОЛДЕР, подвинуть в шлеме.
- SpawnPoint уже был на полу (y=0.18, пол Cube 0.125) — не трогал.
Лобби: PodiumYaroslavl/Socket (StandSocket city=yaroslavl) активен; старый Stand_Yaroslavl остаётся выключенным — ровно 1 активный сокет.
Build Settings: City_Yaroslavl [2], Tanya_Yaroslavl [10] — включены.

ПОТОК (единый): City_Yaroslavl (EVE -> взять камень ПРАВОЙ рукой -> кнопка 'Пройти викторину') -> Tanya_Yaroslavl
  (положить артефакт в держатель=заблокирован; тест -> сейсмограф 'поймать толчки' -> Sismograph.OnAllFixed включает
  quizbase (1)/truee=разблокировка; забрать артефакт -> лобби) -> лобби (PodiumYaroslavl -> PlaceArtifact+CompleteQuiz).
Проверено: консоль чистая (только старые варнинги DoorPan финала); держатель/спавнер/подиум считаны обратно; старые объекты удалены.
ЖИВОЙ ТЕСТ ЗА КОМАНДОЙ (шлем): полный проход Ярославля по держатель-схеме; подвинуть QuizHolder куда удобно.
СТАТУС: по полному циклу готовы Кострома, Ярославль, Владимир, Суздаль, Иваново. Осталось: Ростов, Переславль, Сергиев, Москва.
=== КОНЕЦ ОБНОВЛЕНИЯ 09.10.2026 (Ярославль держатель) ===

>>> ЯРОСЛАВЛЬ: живой тест ПРОЙДЕН (09.10.2026) на держатель-схеме — город ГОТОВ.
    Готовы по полному циклу: Кострома, Ярославль, Владимир, Суздаль, Иваново. Следующий: Переславль.


=== ОБНОВЛЕНИЕ 09.10.2026 (ПЕРЕСЛАВЛЬ: связка город<->квиз<->лобби по шаблону §17) ===
Переславль-Залесский подключён по эталонному шаблону (Владимир). Город собран командой — дособрана плумбинг-часть.

1) CityData City_pereslavl: citySceneName=City_PereslavlZ (ВНИМАНИЕ: сцена города называется City_PereslavlZ, в CityData
   совпадает — проверено), quizSceneName=Tanya_Pereslavl, artifactId=pereslavl_cpu, artifactDisplayName=Процессор — были;
   НАЗНАЧЕН artifactPrefab=Artifact_Pereslavl (был пуст). order=3.
2) Artifact_Pereslavl.prefab СОЗДАН из комнатного ProcessorIntelArtifact (City_PereslavlZ). Компоненты: Rigidbody(kinematic,
   noGravity), BoxCollider(не-триггер, на корне; вогнутых дочерних нет), XRGrabInteractable(layers=Default=1, throwOnDetach=false,
   grab ВКЛ, movementType=Instantaneous), ArtifactPickup(city=pereslavl), StickyArtifact. ВАЖНО (§17.8): артефакт в комнате под
   масштабированным родителем 'room' (~0.3) -> local 0.33 при мировом 0.10. Запёк МИРОВОЙ масштаб (0.10,0.10,0.10) в root префаба,
   иначе был бы в 3.3 раза больше в руке/лобби. Модель тяжёлая (132 рендерера) — при тормозах оптимизировать позже.
3) City_PereslavlZ (команда, НЕ трогал): EVE на EvaZoneGuide (RobotGuide+NavMeshAgent off, useNavMesh=false, city=pereslavl,
   finaleClip=Pereslavl6, 4 стопа), артефакт ProcessorIntelArtifact (видим, grab заблокирован до рассказа), кнопка QuizButton
   -> EvaZoneGuide.GoQuiz (-> Tanya_Pereslavl).
4) Tanya_Pereslavl (сцена чистая, без рига/камеры/EventSystem; старой схемы не было):
   - SpawnPoint поднят на пол: y 0 -> 0.175 (пол Cube под спавном y=0.125).
   - Добавлен ОДИН ArtifactSceneSpawner (ArtifactSpawner; sceneCity/park пустые -> собранный артефакт в руку).
   - Добавлен QuizHolder (скопирован из Tanya_Vladimir): Socket (SphereCollider trigger r=0.15 + XRSocketInteractor
     layers=Everything/-1 + QuizArtifactHolder city=pereslavl, completionIndicator=quizbase (1)/truee). Позиция (2.3,0.24,7.0)
     справа от зоны упорядочивания — ПЛЕЙСХОЛДЕР, подвинуть в шлеме.
   Механика викторины = тест(quizbase) -> упорядочивание шагов алгоритма (Checkly + таблички 1st..5th), индикатор=quizbase (1)/truee.
5) Лобби PodiumPereslavl/Socket (StandSocket city=pereslavl) активен. Build Settings: City_PereslavlZ [8], Tanya_Pereslavl [12] включены.

ПОТОК: City_PereslavlZ (EVE -> взять процессор ПРАВОЙ рукой -> 'Пройти викторину') -> Tanya_Pereslavl (положить в держатель=
  заблокирован; тест + упорядочить шаги; truee разблокирует; забрать -> лобби) -> лобби (PodiumPereslavl -> PlaceArtifact+CompleteQuiz).
Проверено: консоль чистая (только старые варнинги DoorPan); префаб/CityData/спавнер/держатель/подиум считаны обратно.
ЖИВОЙ ТЕСТ ЗА КОМАНДОЙ (шлем): полный проход; подвинуть QuizHolder; ПРОВЕРИТЬ (§17.7) что порядок списка у Checkly совпадает с
  направлением проезда (иначе правильный ответ не засчитается) — это конфиг викторины Тани, не плумбинг.
СТАТУС: по полному циклу готовы Кострома, Ярославль, Владимир, Суздаль, Иваново, Переславль. Осталось: Ростов, Сергиев, Москва.
=== КОНЕЦ ОБНОВЛЕНИЯ 09.10.2026 (Переславль) ===


=== ОБНОВЛЕНИЕ 10.10.2026 (СЕРГИЕВ ПОСАД: связка город<->квиз<->лобби по шаблону §17 + фикс пивота артефакта) ===
Сергиев Посад подключён по эталонному шаблону (Владимир). Готов к полному циклу город->викторина->лобби (живой тест пройден).

1) Artifact_SergievPosad.prefab СОЗДАН из комнатной линзы LensArtifact (City_SergievPosad): grab включён; компоненты по §5
   (Rigidbody kinematic/noGravity, BoxCollider не-триггер, XRGrabInteractable throwOnDetach=false layers=Default(1),
   ArtifactPickup city=sergiev, StickyArtifact). CityData City_sergiev.artifactPrefab назначен (был пуст).
   CityData уже были верны: citySceneName=City_SergievPosad, quizSceneName=Tanya_SergievPosad, artifactId=sergiev_lens.
2) ФИКС ПИВОТА (важно для таких моделей): у линзы пивот далеко от меша -> в руке/сокете артефакт улетал вбок на ~2 м.
   Решение: attachTransform ('Attach') на XRGrabInteractable в ЦЕНТР КОЛЛАЙДЕРА линзы (local ~(-0.006,-0.212,1.361)).
   ВАЖНО: НЕ центр renderer-bounds (там крупные декоративные части утаскивают центр вбок) — именно центр коллайдера.
   XRI фиксирует attach-смещение В МОМЕНТ захвата, поэтому точка обязана быть верна в префабе ДО взятия; на уже
   зажатом экземпляре помогает только повторный захват. Проверять на СВЕЖЕМ спавне. Масштаб префаба уменьшен до 2.25 (по просьбе).
3) Tanya_SergievPosad добрана плумбингом по §17.3: SpawnPoint на пол (y=0.175; пол Cube y=0.125, Plane y~3.6=потолок);
   один ArtifactSceneSpawner (sceneCity/park пустые -> артефакт в руку); QuizHolder (пьедестал-плейсхолдер + Socket:
   SphereCollider trigger r=0.15 + XRSocketInteractor layers=Everything/-1, socketActive + QuizArtifactHolder
   city=sergiev, completionIndicator=quizbase (1)/truee — Planet-викторина 'прицелы/бинокли'). Позиция QuizHolder — плейсхолдер.
4) Лобби PodiumSergiev/Socket (StandSocket city=sergiev, Everything) — уже был верен. Build Settings: City_SergievPosad [18]
   и Tanya_SergievPosad [13] включены.
ГРАБЛЯ ДЛЯ ШАБЛОНА (§17.8 дополнение): артефакт из модели с off-center пивотом (линза и подобное) -> ставь attachTransform
   в ЦЕНТР КОЛЛАЙДЕРА, а не renderer-bounds; иначе в руке/сокете уедет вбок. Эффект виден только на свежем спавне.
ЗАМЕТКА (конфиг Тани, §17.7): порядок Planets у Checkly = [NightVision, binokl, Mikroskop] — сверить с направлением
   проезда блейда, иначе правильный ответ не засчитается (это конфиг викторины, не плумбинг).
Проверено: живой тест ПРОЙДЕН (линза в руке по центру, уменьшена до 2.25); консоль чистая.
=== КОНЕЦ ОБНОВЛЕНИЯ 10.10.2026 (Сергиев Посад) ===

>>> СЕРГИЕВ ПОСАД: живой тест ПРОЙДЕН (10.10.2026) — полный цикл город->викторина->лобби. Город ГОТОВ.
    Готовы по полному циклу: Кострома, Ярославль, Владимир, Суздаль, Иваново, Переславль, Сергиев Посад. Осталось: Ростов, Москва.
