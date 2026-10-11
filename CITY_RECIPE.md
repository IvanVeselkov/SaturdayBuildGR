# РЕЦЕПТ ГОРОДА + АКТУАЛЬНАЯ АРХИТЕКТУРА (образец — Кострома)

> **ЧИТАТЬ ПЕРВЫМ при работе над новым городом.** Этот файл — источник правды по текущей
> архитектуре города и пошаговому созданию следующего. При расхождении со старыми датированными
> блоками в `DEV_PROGRESS.md` (особенно «СЛЕПОК АРХИТЕКТУРЫ 28.09» с RobotGuide/грейбоксом) —
> верен ЭТОТ файл. Порядок чтения в новом чате: PROJECT_BRIEF.md → этот файл → свежие блоки DEV_PROGRESS.md.
> Проверить связь с Unity по MCP безопасной read-командой ДО правок. Обновлено: 05.10.2026.

Движок Unity **6000.5.3f1**, URP. Платформа — **HTC Vive** (десктопный VR). Проект `D:\UnityProjects\GoldenRing`.
Эталонный боевой город = **Кострома** (City_Kostroma). Новый город делается по её образцу.

---

## 1. ПОСТОЯННАЯ ИНФРАСТРУКТУРА (Bootstrap, build index 0) — НЕ дублировать в городах
- GameManager (+ссылка GameConfig.asset) + SceneLoader.
- PlayerRig — инстанс Prefabs/Interaction/PlayerRig.prefab (XR Origin). **Тег рига = "Player"** (для стыковки с проектом Тани).
- XR Interaction Manager; XR Interaction Simulator; EventSystem (+XRUIInputModule).
- Контентные сцены (Lobby/City_*/Quiz_*) грузятся **additive поверх Bootstrap**; одновременно открыта максимум одна.
- Контентная сцена содержит ТОЛЬКО окружение + один `SpawnPoint` (маркер) + интерактив. БЕЗ рига/менеджеров/камеры/EventSystem.

## 2. ЯДРО (переиспользуемые скрипты, НЕ трогать без нужды)
- **GoldenRing.Core**: GameManager (прогресс/разблокировка/финал/сброс), GameConfig (список городов по order), CityData (данные города), SceneLoader (переходы+fade+перенос рига в SpawnPoint), ScreenFader, PlayerRig, SpawnPoint.
  - GameManager API: IsCityUnlocked(city) [city.order<=highestUnlocked], CollectArtifact, PlaceArtifact, CompleteQuiz(city) [открывает следующий], AreAllArtifactsPlaced, ResetProgress. События On*. СТАТИСТИКА ФИНАЛА: RegisterMistake()/Mistakes (хук викторины — звать на каждый неверный ответ), NotifyCityEntered()/ElapsedSeconds (таймер прохождения: старт на 1-м городе, стоп при замыкании кольца), DebugSetStatsForTesting (тест).
  - SceneLoader API: GoToLobby(), GoToCity(CityData), GoToQuiz(CityData), GoToScene(name), GoToFinale(), RestartToLobby(); свойство IsTransitioning.
- **GoldenRing.Interaction**: ArtifactPickup (грэб рукой→CollectArtifact; SetCity()), StandSocket (стенд в лобби, фильтр «свой артефакт», →PlaceArtifact), StickyArtifact (нельзя выронить мимо стенда), ArtifactSceneSpawner (data-driven спавн; в боевых городах с физ-артефактом ВЫКЛЮЧЕН), CityRouteMap (карта-маршрут лобби: состояния/цвета/подписи/пути/золотое кольцо, см. §9) + CityMarker (точка города, клик→прямой GoToCity текущего; MapController устарел, попап отключён), GoToCityButton, RestartGameButton, HeadCollisionPushback (на PlayerRig: голову не пустить сквозь коллайдеры при физ-ходьбе/симуляторе — капсула едет за головой + выталкивание, см. §15).
- **GoldenRing.Guide**: **EvaZoneGuide** (текущий гид, см. §4), RobotGuide+GuideStop (старый radius-based гид — в новых городах НЕ используем), EVE-префаб Prefabs/Guide/EVE.prefab.
- **GoldenRing.Quiz**: QuizStubController (заглушка викторины).
- **GoldenRing.DebugTools**: DebugUnlock (ТЕСТ — открыть города; TEST_DebugUnlock лежит в лобби, **удалить перед сдачей**), DebugFinaleTrigger (ТЕСТ — поставить все артефакты и запустить финал; TEST_FinaleTrigger + кнопка «ТЕСТ: Финал» в лобби, **удалить перед сдачей**), DebugAdvanceCity (ТЕСТ — открыть города по одному: TEST_AdvanceCity + кнопка «ТЕСТ: +1 город» в лобби, **удалить перед сдачей**).
- **ФИНАЛ (GoldenRing.Core)**: FinaleSequenceController (в лобби: по OnAllArtifactsPlaced поднимает раздвижную дверь DoorPanel в воротах FinaleGate (z=-4) → по входу головы в DoorwayZone зовёт GoToFinale; отдельный золотой круг RingGlow УБРАН — замыкание кольца показывает сама золотая дорога маршрута), FinaleStatsScreen (сцена Core/Finale.unity: время прохождения, ошибки, 3 медали с подсветкой заработанной; пороги мин. в полях — дефолт 10/15, дальше бронза; кнопки Пройти заново/Выход). Медали — спрайты Art/UI/Medals/medal_{gold,silver,bronze}.png. Холст финала смотрит на игрока (rotation y=0!). Модели двери/кольца — плейсхолдеры, заменит Никита.

## 3. CityData (Assets/_Project/Data/Cities/City_<id>.asset) — ЗАПОЛНИТЬ для нового города
Поля: id, displayName, order, **citySceneName**, **quizSceneName**, artifactId, artifactDisplayName, **artifactPrefab**.
- Сцена викторины, куда ведёт кнопка EVE «Пройти викторину», берётся из **quizSceneName** (НЕ с кнопки!). Цепочка: кнопка → EvaZoneGuide.GoQuiz() → SceneLoader.GoToQuiz(city) → грузит city.quizSceneName (сцена обязана быть в Build Settings).
- Все 9 CityData уже существуют; для нового города просто заполнить пустые поля (минимум citySceneName/quizSceneName/artifactId/artifactPrefab).

## 4. EvaZoneGuide — ЗОННО-ТРИГГЕРНЫЙ ГИД (как настраивать маршрут EVE)
EVE = инстанс Prefabs/Guide/EVE.prefab. На инстансе: **RobotGuide и NavMeshAgent ВЫКЛЮЧЕНЫ** (enabled=false), добавлен EvaZoneGuide.
Поля EvaZoneGuide:
- Ссылки: audioSource (на EVE), city (CityData), screenUI (дочерний «EVE Screen Canvas»), quizButton (его onClick перевязан на EvaZoneGuide.GoQuiz).
- **intro** = {evaPoint: StartEVAPoint, zone: ZoneToActivateTalkWithEVA (Collider), clip: вступление}. EVE стоит на StartEVAPoint (у входа/снаружи).
- **stops** (экспонаты по порядку). Каждая остановка = {label, evaPoint, zone (Collider), clip, teleportToPoint}:
  - игрок входит в zone → EVE проигрывает clip; перед этим EVE доходит/прыгает на evaPoint.
  - **teleportToPoint=true** = мгновенно (ВАРП). Использовать ТОЛЬКО для входа внутрь комнаты (первый экспонат).
  - **teleportToPoint=false** = идти пешком.
  - **ВЕЙПОИНТ** (точка-поворот маршрута без рассказа) = остановка с zone=null и clip=null: EVE просто проходит её и идёт дальше. Ставить вейпоинты, чтобы вести EVE через проходы в обход стен.
- **taskClip** (реплика «найди артефакт»), **finaleClip** (реплика перед викториной), **artifact** (GameObject физ-артефакта — ждём его сбора).
- **useNavMesh**: false = EVE идёт ПО ПРЯМОЙ между точками (маршрут задаёшь точками/вейпоинтами вручную) — так у Костромы, т.к. комната разбита стенами на секции и навмеш-острова не связаны. true = движение по NavMesh (agent.SetDestination) — для городов со СВЯЗНЫМ полом.
- Поведение: ждёт конца перехода (SceneLoader.IsTransitioning), чтобы intro не сработал раньше; детект игрока в зоне — через collider.bounds.Contains; поворот к игроку/по движению; faceYawOffset=90 (разворот модели EVE).

Маршрут Костромы (useNavMesh=false): Цейс (EVApoint2, teleport=true ВХОД) → метеорит (EVApoint3, пешком) → вейпоинт EVApoint3.1 → вейпоинт EVApoint3.2 → Спутник (EVApoint4, пешком). Проверяй, что прямые отрезки между точками свободны от стен (рейкаст на y≈0.9).

## 5. АРТЕФАКТ
Физическая модель-экспонат (у Костромы — meteor). На неё: Rigidbody(isKinematic=true,useGravity=false) + Collider(Box/Mesh) + XRGrabInteractable(interactionLayers=Default=1, throwOnDetach=false) + ArtifactPickup(SetCity(city)) + StickyArtifact. Грэб **ПРАВОЙ рукой** → CollectArtifact(city) (см. §15 — схема управления Vive).
**Блокировка до рассказа:** EvaZoneGuide на старте выключает XRGrabInteractable артефакта (SetArtifactGrabbable(false)) и включает только ПОСЛЕ реплики-задания (taskClip «найди гостя из космоса»). Так артефакт нельзя взять раньше времени.
ArtifactSceneSpawner в боевой сцене города ВЫКЛЮЧИТЬ (artifact = физ-модель, не спавним).
НЕСТЫКОВКА (открыто): в лобби/викторине артефакт пока data-driven Artifact_Kostroma (сфера-плейсхолдер), а в комнате — модель Никиты. Свести: назначить префабу Artifact_<city> модель экспоната.

## 6. ТЕЛЕПОРТ (вход в комнату) — частые грабли
- Платформа входа = объект с **TeleportationAnchor**. КРИТИЧНО: список **colliders у якоря должен содержать его Collider** (anchor.colliders=[BoxCollider]) — иначе луч телепорта по платформе не попадает и телепорт «не работает». interactionLayers=Default(1); teleportTrigger=OnSelectExited; **teleportAnchorTransform = SpawnpointInRoom** (точка приземления внутри комнаты).
- Пол комнаты (walkable) должен иметь **TeleportationArea** (interactionLayers=1), иначе внутри нельзя перемещаться телепортом.
- Рабочее значение interaction-слоя телепорт-поверхностей = Default (value=1), как у пола песочницы.

## 7. МАРКЕРЫ/ЗОНЫ (прятать от игрока)
- Зоны ZoneToActivate* : Collider isTrigger=true, MeshRenderer выключен.
- Точки EVApoint*/StartEVAPoint : MeshRenderer выключен, Collider выключен (чистые маркеры, читается только transform).

## 8. СЦЕНА ВИКТОРИНЫ
Стаб Quiz_<city> (QuizStubController: CompleteAndReturn()=CompleteQuiz+GoToLobby; BackToCity()). Когда придёт викторина Тани — заменить стаб её логикой (держатель артефакта запускает викторину; по успеху CompleteQuiz; забрать артефакт обратно в руку). Имя сцены — в CityData.quizSceneName + в Build Settings.

## 9. ЛОББИ
КАРТА-МАРШРУТ (скрипт CityRouteMap на объекте Map, фон — спутник Art/UI/map_lobby.png на MapWall). Пошаговое открытие по прогрессу:
- маркер ПРОЙДЕН (IsQuizCompleted) → зелёный, не кликабелен; ТЕКУЩИЙ (открыт, не пройден) → жёлтый, клик → сразу SceneLoader.GoToCity (без попапа); ЕЩЁ ЗАКРЫТ → скрыт.
- подписи — ПОСТОЯННЫЕ объекты сцены Map/Labels/Label_<id> (TMP 3D, Arial SDF, rotation=identity, видны в редакторе); CityRouteMap их не создаёт, а находит по имени и только включает/выключает по видимости;
- серый путь (LineRenderer) между соседними городами появляется при открытии следующего; когда пройдены ВСЕ — все пути золотые + замыкающий отрезок (последний→первый).
- клик входит только в город с готовой сценой в билде (Application.CanStreamedLevelBeLoaded). Маркеры Marker_<id> — дети Map, расставлены по географии (опора — Кострома).
- ПОРЯДОК ГОРОДОВ (CityData.order / GameConfig): 0 Кострома, 1 Ярославль, 2 Ростов, 3 Переславль, 4 Сергиев Посад, 5 Москва, 6 Владимир, 7 Суздаль, 8 Иваново. Кострома открыта на старте (TEST_DebugUnlock выключен, для Костромы не нужен).
- Старый MapController/«Map Popup» ОТКЛЮЧЁН (SetActive=false; скрипт не используется).
Для нового города: добавить **Stand_<city>** (клон стенда, StandSocket.city=город) рядом с остальными. Стенд-префаб от Никиты — в Prefabs/Stands.

## 10. BUILD SETTINGS
Bootstrap(0), Lobby(1), City_*(…), Quiz_*(…), Finale (экран итогов, добавлен 03.10). Песочницы (_Sandbox) — выключены (галка снята). Новый город+его викторину добавить и включить.

## 11. КОМНАТА ОТ НИКИТЫ (модель здания/комнаты)
Если комната «пустая внутри» (видно насквозь): материалы перевести в двусторонние (_Cull=0 + doubleSidedGI). Коллайдеры: у импортёра модели включить addCollider (MeshCollider на пол/стены). НАДЁЖНЕЕ (03.10): поверх импортного MeshCollider стен добавить ПРОСТЫЕ BoxCollider по периметру — непробиваемы для continuous-move и телепорт-луча. В Костроме: корневой объект «Colliders» → RoomWall_* (зал), Building_* (кольцо наружного здания node_0), + BoxCollider на сегменты ограды KostromaMainBuilding/01_2* (забор, чтобы не покинуть территорию). ВАЖНО: физическую room-scale ходьбу коллайдеры в VR не блокируют. Варнинги «self-intersecting polygon discarded» — геометрия из Блендера, не критично (чистится в Блендере: Merge by Distance + Triangulate + Recalculate Normals).

## 12. ПОШАГОВО: ДОБАВИТЬ НОВЫЙ ГОРОД (образец — Кострома)
1. Заполнить City_<id>.asset (citySceneName, quizSceneName, artifactId, artifactPrefab).
2. Получить от Никиты модель здания+комнаты; поставить в сцену; починить материалы/коллайдеры (§11).
3. Создать сцену City_<id>: окружение (здание+комната) + SpawnPoint (снаружи) + SpawnpointInRoom (внутри) + платформа-телепорт (TeleportationAnchor → §6) + TeleportationArea на пол комнаты.
4. Разложить экспонаты на стендах; один из них — артефакт (§5).
5. Поставить EVE (инстанс префаба), выключить RobotGuide+NavMeshAgent, добавить EvaZoneGuide; расставить EVApoint'ы, ZoneToActivateEva'ы и (при стенах) вейпоинты; заполнить EvaZoneGuide (§4). Клипы озвучки — Audio/Guide/<City>/.
6. Спрятать меши зон/точек (§7). ArtifactSceneSpawner в сцене выключить.
7. Создать Quiz_<id> (стаб или викторина Тани). Добавить City_<id> и Quiz_<id> в Build Settings.
8. В лобби добавить Stand_<id>.
9. Тест в Play из Bootstrap (можно через TEST_DebugUnlock открыть город). Консоль должна быть чистой.

## 13. ГРАБЛИ РАБОТЫ ЧЕРЕЗ MCP (важно!)
- Правка .cs: редактировать НА МЕСТЕ (guid сохраняется); не delete+create.
- **После правки скрипта через MCP кликнуть по окну Unity** — иначе редактор (без фокуса) не дожимает перекомпиляцию и домен не перезагружается (новое поле «не появляется»).
- Назначая ссылки на ИНСТАНСЕ (EVE.city, hidePoint, stops и т.п.): ОБЯЗАТЕЛЬНО EditorUtility.SetDirty + MarkSceneDirty + SaveScene, иначе при реимпорте префаба сбрасываются в null.
- Русский UI → шрифт Art/Fonts/Arial SDF (дефолтный LiberationSans без кириллицы).
- EVE исключена из запекания навмеша (NavMeshModifier ignoreFromBuild на префабе).
- execute_code компилятор = C# 6 (codedom): без локальных функций и без `using` внутри тела.

## 15. СХЕМА УПРАВЛЕНИЯ (HTC Vive)
- **ЛЕВАЯ рука — локомоция/ходьба** (телепорт). **ПРАВАЯ рука — хват артефактов.**
- PlayerRig.prefab (интеракторы Near-Far, слои: Default=1 хват, Map=2 маркеры карты):
  - Left Controller / Near-Far interactionLayers = **2 (Map)** — артефакты НЕ берёт, жмёт маркеры.
  - Right Controller / Near-Far interactionLayers = **3 (Default+Map)** — берёт артефакты и жмёт маркеры.
  - PlayerRig.leftHandInteractor («держащая рука» для force-select артефакта спавнером/StickyArtifact) = **Right Controller Near-Far**.
  - Телепорт-интеракторы на обоих контроллерах (слой Teleport, бит 31) — не трогаем.
- NB: имя поля `leftHandInteractor`/`LeftInteractor` историческое — фактически указывает на ПРАВУЮ руку (держащую). StickyArtifact возвращает артефакт в эту руку.
- КОЛЛИЗИЯ ГОЛОВЫ (важно!): стик (DynamicMoveProvider→CharacterController) упирается в коллайдеры сам. НО физическая room-scale ходьба и движение в XR Device Simulator двигают ПОЗУ КАМЕРЫ мимо CharacterController — коллайдеры их НЕ держат. Поэтому на PlayerRig висит HeadCollisionPushback: капсула CharacterController едет под голову, при заходе головы в геометрию риг выталкивается (OverlapCapsule+ComputePenetration, только XZ). Так голову нельзя просунуть сквозь стену любым способом. Если «проходит сквозь» — проверять ЭТОТ скрипт (включён, origin/controller назначены, collisionLayers включает слой стен), а не только коллайдеры.

## 16. NAVMESH — ЗАПЕКАТЬ ТОЛЬКО ПО МАРШРУТУ EVE
- У Костромы EVE ходит по точкам (useNavMesh=false), навмеш для движения не нужен, поэтому запекаем МИНИМУМ.
- Один NavMeshSurface на отдельном объекте **EvaNavZone** (в origin, identity), collectObjects=**Volume**, center/size = коробка вокруг коридора маршрута EVE (у Костромы center≈(3.1,1,8.25), size≈(9.5,4,6.5)). ~50 вершин вместо тысяч.
- НЕ использовать collectObjects=All (печёт всю территорию, включая общий «пол»-Cube 50×34). Удалить лишние NavMeshSurface (был на грейбокс-InteriorFloor).
- Если для города EVE будет ходить по навмешу (useNavMesh=true) — объём Volume должен покрывать связный проход между секциями.

## 14. ОТКРЫТЫЕ ХВОСТЫ (доделать)
- Свести вид артефакта лобби/викторина (сфера) ↔ комната (модель).
- Вставить реальную викторину Тани вместо стаба (+ стыковка по тегу Player). При интеграции добавить вызов GameManager.RegisterMistake() на каждый неверный ответ (для статистики финала).
- Удалить TEST_DebugUnlock И TEST_FinaleTrigger (+кнопка «ТЕСТ: Финал») из лобби перед сдачей.
- Финал: заменить плейсхолдеры (дверь FinaleGate/DoorPanel, кольцо RingGlow) моделями Никиты; при желании назначить звук замыкания (sfx/ringClip на FinaleSequenceController).
- Грейбокс-леготовки City_Kostroma (Facade/InteriorFloor/Exhibit_*/Stop_*) ВЫКЛЮЧЕНЫ 03.10 (SetActive=false). Plaza ОСТАВЛЕНА (спавн + TeleportationArea). Коллайдеры здания/зала/забора — простые BoxCollider (см. §11).
- Ярославль переведён на EvaZoneGuide и ЗАВЕРШЁН (05.10.2026, см. DEV_PROGRESS). Готовы: Кострома, Ярославль, Иваново. Дальше — клонировать схему на остальные города.


## 17. ПОДКЛЮЧЕНИЕ СЦЕНЫ ВИКТОРИНЫ ТАНИ (Tanya_<City>) К ГОРОДУ И ЛОББИ — ШАБЛОН
> Проверенные образцы: КОСТРОМА и ВЛАДИМИР. Делать по шагам для каждого нового города.
> Полный поток игрока: ГОРОД (взять артефакт → в руку; EVE предлагает викторину) → КВИЗ (положить артефакт
> в держатель = заблокирован; пройти интерактив; панель 'верно' разблокирует; забрать артефакт → лобби) →
> ЛОББИ (поставить артефакт на подиум → откроется следующий город).

### 17.1 Данные и префаб артефакта
- CityData (Data/Cities/City_<id>.asset): citySceneName=City_<City>, quizSceneName=Tanya_<City>, artifactId,
  artifactDisplayName, **artifactPrefab** (см. ниже). Без artifactPrefab спавнер НИЧЕГО не создаёт — артефакт
  не появится ни в руке, ни на подиуме.
- Artifact_<City>.prefab (Prefabs/Artifacts/): модель артефакта города + компоненты: Rigidbody(isKinematic=true,
  useGravity=false), Collider по мешу, XRGrabInteractable(throwOnDetach=false, interactionLayers=Default(1), ВКЛ),
  ArtifactPickup(city=<id>), StickyArtifact. Проще всего: дублировать артефакт из City-сцены, включить grab,
  SaveAsPrefabAsset, назначить в CityData.artifactPrefab. (Так сделан Artifact_Vladimir из модели шлема.)

### 17.2 Сцена города City_<City>
- EVE на EvaZoneGuide (RobotGuide+NavMeshAgent off); артефакт-модель в комнате с ArtifactPickup(city)+StickyArtifact
  +XRGrabInteractable (grab блокируется до рассказа, EVE включает после). Кнопка викторины → EvaZoneGuide.GoQuiz
  (ведёт в city.quizSceneName). Строит команда в шлеме (см. §12).

### 17.3 Сцена викторины Tanya_<City>
a. SpawnPoint — на ПОЛ. Рейкастом вниз найти пол: это Cube (обычно y≈0.13), поставить SpawnPoint на y=пол+0.05.
   ВНИМАНИЕ: Plane на y≈3.7 — это ПОТОЛОК/бэкдроп, НЕ пол (ошибётесь — игрок спавнится на потолке/в пустоте).
b. Ровно ОДИН объект с ArtifactSceneSpawner (поля sceneCity и parkCollectedHere пустые/false) — артефакт
   приедет в руку. НЕ дублировать спавнер: два = КЛОН артефакта (один ставишь, второй в руке).
c. Держатель QuizHolder = пьедестал (визуал) + дочерний объект Socket:
   - SphereCollider(isTrigger=true, r≈0.15 world);
   - XRSocketInteractor(interactionLayers=Everything/-1, socketActive=true);
   - QuizArtifactHolder(city=<id>, completionIndicator = панель 'верно' интерактивной викторины).
   Поставить в интерактивной комнате (куда игрок доходит с артефактом в руке).
d. completionIndicator: у Planet-викторин (сопоставление/сборка) — объект Planet.truee (обычно quizbase (1)/truee);
   у сейсмографа — объект, который включает Sismograph.OnAllFixed.
e. Кнопку 'Вернуться в лобби' НЕ добавлять: возврат происходит по ЗАБОРУ артефакта из держателя.

### 17.4 Лобби
- Podium<City>/Socket = XRSocketInteractor(interactionLayers=Everything/-1) + StandSocket(city=<id>).
- Постановка артефакта на подиум сама вызывает PlaceArtifact + CompleteQuiz (открывает следующий город) —
  уже в коде StandSocket.OnSelectEntered. Отдельной кнопки не нужно.

### 17.5 Build Settings
- Включить City_<City> и Tanya_<City>.

### 17.6 Как это работает в коде (не менять без нужды)
- ArtifactSceneSpawner: единственный источник правды — состояние GameManager; на старте УНИЧТОЖАЕТ все имеющиеся
  ArtifactPickup в сцене, затем раскладывает артефакт по данным (в руку / на стенд) — защита от клонов и 'выживших'.
- QuizArtifactHolder: пока викторина не пройдена, ставит артефакту interaction-слой (1<<20), которого НЕТ у рук
  (Default+Map) — сокет (Everything) держит, рука взять не может. По активации completionIndicator возвращает
  обычный слой; забор артефакта из держателя → SceneLoader.GoToLobby(). StickyArtifact на артефакте при этом отключается.
- StandSocket (подиум лобби): фильтр по artifactId, при постановке PlaceArtifact + CompleteQuiz.

### 17.7 Грабли (уже ловили)
- Planet-викторина: порядок списка Planets у Checkly ДОЛЖЕН совпадать с направлением проезда блейда Checkly по X
  (он едет справа налево, с +X к -X). Если список в обратном порядке — правильная расстановка НЕ засчитается.
  Каждая табличка числится в списке дважды (у неё два коллайдера → два OnTriggerEnter) — это штатно.
- Два ArtifactSceneSpawner в сцене = клон артефакта.
- HeadCollisionPushback (тест-скрипт выталкивания головы) ОТКЛЮЧЁН на PlayerRig (в финале не нужен) —
  из-за него артефакт в руке мог неконтролируемо толкать игрока. Не включать обратно.
- SpawnPoint на потолочный Plane (y≈3.7) вместо пола (Cube y≈0.13) — частая ошибка.

### 17.8 Грабли с АРТЕФАКТОМ-префабом и держателем (ловили на Суздале 09.10.2026)
- ПЛОСКИЙ артефакт в квизе/лобби: если модель артефакта в сцене города лежит под масштабированным родителем, её мировой
  масштаб = local * lossyScale родителя. Префаб Artifact_<city> хранит только ЛОКАЛЬНЫЙ масштаб -> отдельно выглядит искажённым.
  Решение: запечь МИРОВОЙ масштаб (brevnoArtifact.lossyScale) в root.localScale префаба. Пример Суздаля: (32.82,15.56,15.56).
- ВОГНУТЫЕ дочерние MeshCollider (часто 'Plane'/'Plane (1)' из импорта) на грэбабельной модели: при захвате XRGrabInteractable
  (movementType=VelocityTracking) тело становится динамическим -> PhysX сыплет 'Concave Mesh Colliders not supported with dynamic
  Rigidbody', 'computeMassAndInertia illegal shapes', 'Physics.ClosestPoint ... convex'. Решение: удалить вогнутые дочерние
  MeshCollider, оставить корневой BoxCollider (§5). Сборку Loga это не ломает (ловит по тегу на корне).
- isTrigger У АРТЕФАКТА = НЕ БЕРЁТСЯ ЛУЧОМ: дальний луч (NearFar) игнорирует триггер-коллайдеры, поэтому если у артефакта
  isTrigger=true, навестись/взять лучом нельзя (можно только прямым near-грэбом). На Суздале коллайдер включался в триггер в
  рантайме (источник не локализован: ни наш код, ни префаб, ни боевой XRI его не ставят). СТРАЖ в QuizArtifactHolder
  (ForceCollidersSolid) теперь держит коллайдеры артефакта не-триггерами, пока он в держателе — часть шаблона, доп. настройки не нужно.
- ИНТРО EVE: чтобы приветствие (фраза 1) звучало сразу при спавне — оставляй intro.zone ПУСТЫМ (как Ярославль/Сергиев).
  Если задать зону далеко от SpawnPoint, игрок в неё не заходит и рассказ не стартует.
- ЧЁРНАЯ ДВЕРЬ между тестом и интерактивом: у кнопки 'продолжить' на панели quizbase/CloseQuiz должен быть onClick-вызов
  Animator.Play('Close') с целью = Animator двери (объект 'Cube'). Клип 'Close' = анимация ОТКРЫТИЯ (стена уезжает). Если цель
  вызова пустая (null) — дверь не откроется.
- City_<City> и Tanya_<City> ОБА должны быть включены в Build Settings (на Суздале City_Suzdal забыли — вход в город не работал).
