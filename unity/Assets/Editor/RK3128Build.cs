using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RK3128Build
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ApkPath = "E:/game-pro/apk-test/AnimalRace_RK3128_V32_Redrawn.apk";
    private const string PcPath = "E:/game-pro/pc-test/AnimalRace_PC_V32/AnimalRace_PC_V32.exe";
    private const string FontPath = "Assets/Fonts/simhei.ttf";
    private const string ArcadeFontPath = "Assets/Fonts/arial-rounded-bold.ttf";
    private const string OriginalArtRoot = "Assets/Art/RecoveredOriginal";
    private const string OriginalSourceRoot = OriginalArtRoot + "/Source";
    private const string OriginalProcessedRoot = OriginalArtRoot + "/Processed";
    private const string AarPath = "Assets/Plugins/Android/serialplugin-release.aar";
    private const string KotlinStdlibPath = "Assets/Plugins/Android/kotlin-stdlib-2.2.10.jar";
    private const string AndroidSdkPath = "E:/android/SdkUnity2018";
    private const string JdkPath = "E:/unity/2018.4.36f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK";

    private static readonly string[] AnimalNames =
    {
        "lion", "elephant", "gorilla", "zebra", "giraffe", "camel"
    };

    [MenuItem("Animal Race/V3.2 Rebuild Scene")]
    public static void ConfigureSceneOnly()
    {
        PrepareRecoveredOriginalAssets();
        ConfigureArtAssets();
        ConfigureScene();
        ConfigureCommonPlayer();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Animal Race/Build V3.2 RK3128 APK")]
    public static void ConfigureAndBuild()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        ConfigureExternalTools();
        ConfigureAndroidPlugins();
        ConfigureSceneOnly();
        ConfigureAndroidPlayer();

        BuildPlayer(ApkPath, BuildTarget.Android);
    }

    [MenuItem("Animal Race/Build V3.2 PC Simulator")]
    public static void ConfigureAndBuildPc()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PcPath));
        ConfigureSceneOnly();
        PlayerSettings.defaultIsFullScreen = false;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
        BuildPlayer(PcPath, BuildTarget.StandaloneWindows64);
    }

    public static void BuildGd32SynchronizedApk(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ConfigureExternalTools();
        ConfigureAndroidPlugins();
        ConfigureAndroidPlayer();
        BuildPlayer(path, BuildTarget.Android);
    }

    private static void BuildPlayer(string path, BuildTarget target)
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new System.Exception("Animal Race build failed: " + report.summary.result);
        }
    }

    private static void ConfigureScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var mainGame = new GameObject("MainGameScene");

        var cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(mainGame.transform, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.10f, 0.12f);
        camera.orthographic = true;
        cameraObject.tag = "MainCamera";

        var font = LoadRequired<Font>(FontPath);
        var arcadeFont = LoadRequired<Font>(ArcadeFontPath);

        var serialReceiver = new GameObject("SerialReceiver");
        serialReceiver.transform.SetParent(mainGame.transform, false);
        serialReceiver.AddComponent<MainThreadMessageQueue>();
        var serialService = serialReceiver.AddComponent<AndroidSerialService>();

        var canvas = new GameObject("Canvas");
        canvas.transform.SetParent(mainGame.transform, false);
        var panelObject = new GameObject("SerialDebugPanel");
        panelObject.transform.SetParent(canvas.transform, false);
        var debugPanel = panelObject.AddComponent<SerialDebugPanel>();
        debugPanel.uiFont = font;
        debugPanel.SerialService = serialService;
        debugPanel.StartVisible = false;
        serialService.DebugPanel = debugPanel;

        var displayObject = new GameObject("DisplayStateController");
        displayObject.transform.SetParent(mainGame.transform, false);
        var displayController = displayObject.AddComponent<DisplayStateController>();
        displayController.uiFont = font;
        displayController.SerialService = serialService;
        displayController.DebugPanel = debugPanel;
        displayController.ShowLegacyUi = false;

        var bootstrap = mainGame.AddComponent<AppBootstrap>();
        bootstrap.SerialService = serialService;
        bootstrap.DisplayController = displayController;

        var presentation = new GameObject("GamePresentation");
        presentation.transform.SetParent(mainGame.transform, false);
        var animationController = presentation.AddComponent<AnimalRaceAnimationController>();
        animationController.DisplayController = displayController;
        animationController.SerialService = serialService;

        var resultDisplay = presentation.AddComponent<ResultDisplay>();
        resultDisplay.DisplayController = displayController;
        resultDisplay.SerialService = serialService;
        var payoutDisplay = presentation.AddComponent<PayoutDisplay>();
        payoutDisplay.DisplayController = displayController;
        var faultDisplay = presentation.AddComponent<FaultDisplay>();
        faultDisplay.DisplayController = displayController;
        var settingsDisplay = presentation.AddComponent<SettingsDisplay>();
        settingsDisplay.DisplayController = displayController;
        settingsDisplay.UiFont = font;
        settingsDisplay.TitleAtlas = LoadProcessed("settings_labels");
        settingsDisplay.CursorArtwork = LoadProcessed("settings_cursor");
        settingsDisplay.NavigationHints = LoadProcessed("settings_navigation_hints");
        settingsDisplay.SingleMachineMenuAtlas = LoadProcessed("settings_single_menu");
        settingsDisplay.ParameterLabelsAtlas = LoadProcessed("settings_parameter_labels");
        settingsDisplay.AccountLabelsAtlas = LoadProcessed("settings_account_labels");
        settingsDisplay.PasswordPanelArtwork = LoadProcessed("settings_password_panel");
        settingsDisplay.SystemLabelsAtlas = LoadProcessed("settings_system_labels");
        var winCollectPresenter = presentation.AddComponent<WinCollectPresenter>();
        winCollectPresenter.DisplayController = displayController;

        var gameUi = presentation.AddComponent<AnimalRaceUI>();
        gameUi.uiFont = font;
        gameUi.arcadeFont = arcadeFont;
        gameUi.scoreFont = LoadRequired<Font>("Assets/Fonts/arialbd.ttf");
        gameUi.originalBetDigits = LoadProcessed("ui_bet_digits");
        gameUi.originalPlaqueDigits = LoadProcessed("ui_plaque_digits");
        gameUi.originalHistoryPanel = LoadProcessed("ui_history_panel");
        gameUi.animalRunSheets = LoadRecoveredAnimals("run");
        gameUi.animalIdleSheets = LoadRecoveredAnimals("idle");
        gameUi.animalObstacleSheets = LoadRecoveredAnimals("actions");
        gameUi.animalPortraits = new Texture2D[0];
        gameUi.animalRunFrames = new Texture2D[0];
        gameUi.animalFallFrames = new Texture2D[0];
        gameUi.animalJumpFrames = new Texture2D[0];
        gameUi.animalBallFrames = new Texture2D[0];
        gameUi.sceneBackgrounds = new Texture2D[0];
        gameUi.originalSceneTopStrips = new[]
        {
            LoadRequired<Texture2D>(OriginalSourceRoot + "/scene_desert_top.png"),
            LoadRequired<Texture2D>(OriginalSourceRoot + "/scene_jungle_top.png"),
            LoadRequired<Texture2D>(OriginalSourceRoot + "/scene_castle_top.png")
        };
        gameUi.originalSceneTrackTiles = new[]
        {
            LoadRequired<Texture2D>(OriginalSourceRoot + "/scene_desert_track.png"),
            LoadRequired<Texture2D>(OriginalSourceRoot + "/scene_jungle_track.png"),
            LoadRequired<Texture2D>(OriginalSourceRoot + "/scene_castle_track.png")
        };
        gameUi.hayWallTexture = LoadProcessed("prop_hay");
        gameUi.hayFragmentsTexture = null;
        gameUi.lilyPadPoolTexture = LoadProcessed("prop_lily");
        gameUi.beachBallStackTexture = LoadProcessed("prop_balls");
        gameUi.beachBallSingleTexture = null;
        gameUi.originalAvatarFrameStrip = LoadProcessed("ui_avatar_frames");
        gameUi.originalPortraitStrip = LoadProcessed("ui_portraits_hd_sheet");
        gameUi.originalBetBoxStrip = LoadProcessed("ui_bet_boxes");
        gameUi.originalWinLabel = LoadProcessed("ui_win");
        gameUi.originalBonusLabel = LoadProcessed("ui_bonus");
        gameUi.originalCreditLabel = LoadProcessed("ui_credit");
        gameUi.originalTopDigits = LoadProcessed("ui_top_digits");
        gameUi.originalOddsGlyphs = LoadProcessed("ui_odds");
        gameUi.originalSmallPlaque = LoadProcessed("ui_small_plaque");
        gameUi.originalFinishLine = LoadProcessed("ui_finish_line");
        gameUi.originalFinishRanks = LoadProcessed("ui_finish_ranks");
        gameUi.DisplayController = displayController;
        gameUi.AnimationController = animationController;
        gameUi.ResultOverlay = resultDisplay;
        gameUi.PayoutOverlay = payoutDisplay;
        gameUi.FaultOverlay = faultDisplay;
        gameUi.SettingsOverlay = settingsDisplay;
        gameUi.WinCollectOverlay = winCollectPresenter;

        CreateGameHierarchy(mainGame.transform);

        var simulatorObject = new GameObject("PcStandaloneSimulator");
        simulatorObject.transform.SetParent(mainGame.transform, false);
        var pcSimulator = simulatorObject.AddComponent<PcStandaloneSimulator>();
        pcSimulator.uiFont = font;
        pcSimulator.DisplayController = displayController;
        pcSimulator.AnimationController = animationController;
        pcSimulator.ResultOverlay = resultDisplay;
        pcSimulator.GameUi = gameUi;
        pcSimulator.SettingsOverlay = settingsDisplay;

        var protocolSimulator = simulatorObject.AddComponent<H743MessageSimulator>();
        protocolSimulator.uiFont = font;
        protocolSimulator.DisplayController = displayController;
        protocolSimulator.enabled = false;
        var scenarioRunner = simulatorObject.AddComponent<ProtocolScenarioRunner>();
        scenarioRunner.DisplayController = displayController;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static Texture2D[] LoadRecoveredAnimals(string pose)
    {
        var textures = new Texture2D[AnimalNames.Length];
        for (int animal = 0; animal < AnimalNames.Length; animal++)
        {
            string name = AnimalNames[animal];
            textures[animal] = LoadRequired<Texture2D>(
                OriginalProcessedRoot + "/Animals/" + name + "_" + pose + ".png");
        }
        return textures;
    }

    private static Texture2D LoadProcessed(string name)
    {
        return LoadRequired<Texture2D>(OriginalProcessedRoot + "/" + name + ".png");
    }

    private static void PrepareRecoveredOriginalAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var sources = new string[AnimalNames.Length];
        var runs = new string[AnimalNames.Length];
        var idles = new string[AnimalNames.Length];
        for (int index = 0; index < AnimalNames.Length; index++)
        {
            string name = AnimalNames[index];
            sources[index] = OriginalSourceRoot + "/animal_" + name + "_sheet.png";
            runs[index] = OriginalProcessedRoot + "/Animals/" + name + "_run.png";
            idles[index] = OriginalProcessedRoot + "/Animals/" + name + "_idle.png";
        }
        RecoveredOriginalAssetProcessor.ProcessAnimals(sources, runs, idles);
        for (int index = 0; index < AnimalNames.Length; index++)
        {
            RecoveredOriginalAssetProcessor.ProcessObstaclePoses(sources[index],
                OriginalProcessedRoot + "/Animals/" + AnimalNames[index] + "_actions.png");
        }

        string[] transparentNames =
        {
            "prop_balls", "prop_hay", "prop_lily", "ui_avatar_frames",
            "ui_portraits", "ui_bet_boxes", "ui_win", "ui_bonus", "ui_credit",
            "ui_top_digits", "ui_odds", "ui_small_plaque", "ui_finish_line",
            "settings_cursor", "ui_bet_digits", "ui_plaque_digits", "ui_history_panel"
        };
        for (int index = 0; index < transparentNames.Length; index++)
        {
            string name = transparentNames[index];
            RecoveredOriginalAssetProcessor.PrepareTransparentCopy(
                OriginalSourceRoot + "/" + name + ".png",
                OriginalProcessedRoot + "/" + name + ".png");
        }
        string[] allBlackTransparentNames =
        {
            "settings_navigation_hints", "settings_labels", "settings_single_menu",
            "settings_parameter_labels", "settings_account_labels", "settings_password_panel",
            "settings_root_labels", "settings_prompt_text", "settings_help_text", "settings_system_labels",
            "settings_multiplier_choices"
        };
        for (int index = 0; index < allBlackTransparentNames.Length; index++)
        {
            string name = allBlackTransparentNames[index];
            RecoveredOriginalAssetProcessor.PrepareAllBlackTransparentCopy(
                OriginalSourceRoot + "/" + name + ".png",
                OriginalProcessedRoot + "/" + name + ".png");
        }
        RecoveredOriginalAssetProcessor.PrepareFinishRankCopy(
            OriginalSourceRoot + "/ui_finish_ranks.png",
            OriginalProcessedRoot + "/ui_finish_ranks.png");
    }

    private static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new System.Exception("Required asset not found: " + path);
        return asset;
    }

    private static void ConfigureArtAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { OriginalArtRoot });
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool background = path.Contains("/scene_");
            bool sourceSheet = path.Contains("_sheet.png");
            bool avatarFrames = path.EndsWith("/ui_avatar_frames.png");
            bool highResolutionPortraits = path.EndsWith("/ui_portraits_hd_sheet.png");
            if(highResolutionPortraits || path.EndsWith("/ui_portraits.png")) importer.isReadable=true;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = !background;
            importer.mipmapEnabled = false;
            importer.wrapMode = background ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = avatarFrames ? FilterMode.Point : FilterMode.Bilinear;
            importer.maxTextureSize = highResolutionPortraits ? 4096 :
                (background || sourceSheet || path.Contains("_actions.png") ? 2048 : 1024);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureAndroidPlugins()
    {
        AssetDatabase.ImportAsset(AarPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(KotlinStdlibPath, ImportAssetOptions.ForceUpdate);
        ConfigureAndroidPlugin(AarPath);
        ConfigureAndroidPlugin(KotlinStdlibPath);
    }

    private static void ConfigureAndroidPlugin(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
        if (importer == null) throw new System.Exception("Android plugin importer not found: " + assetPath);
        importer.SetCompatibleWithAnyPlatform(false);
        importer.SetCompatibleWithEditor(false);
        importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
        importer.SaveAndReimport();
    }

    private static void ConfigureExternalTools()
    {
        EditorPrefs.SetString("AndroidSdkRoot", AndroidSdkPath);
        EditorPrefs.SetString("AndroidSdkRootR16", AndroidSdkPath);
        EditorPrefs.SetString("JdkPath", JdkPath);
        System.Environment.SetEnvironmentVariable("ANDROID_HOME", AndroidSdkPath);
        System.Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", AndroidSdkPath);
        System.Environment.SetEnvironmentVariable("JAVA_HOME", JdkPath);
        System.Environment.SetEnvironmentVariable("GRADLE_OPTS", "-Dorg.gradle.daemon=false");
    }

    private static void ConfigureCommonPlayer()
    {
        PlayerSettings.productName = "AnimalRaceV32";
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.MTRendering = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static void ConfigureAndroidPlayer()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.example.animalrace");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel19;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 });
    }

    private static void CreateGameHierarchy(Transform parent)
    {
        CreateChild(parent, "GameBackground");
        Transform topStatusBar = CreateChild(parent, "TopStatusBar");
        CreateChild(topStatusBar, "WinText");
        CreateChild(topStatusBar, "BonusText");
        CreateChild(topStatusBar, "BalanceText");
        Transform raceArea = CreateChild(parent, "RaceArea");
        for (int index = 1; index <= AnimalCatalog.Count; index++)
        {
            CreateChild(raceArea, "Track_" + index.ToString("00"));
            CreateChild(raceArea, AnimalCatalog.GetResourceName(index));
        }
        Transform bettingPanel = CreateChild(parent, "BettingPanel");
        for (int index = 1; index <= AnimalCatalog.Count; index++)
        {
            CreateChild(bettingPanel, "AnimalSlot_" + index.ToString("00"));
        }
        CreateChild(parent, "IdleHintPanel");
        CreateChild(parent, "ResultOverlay");
        CreateChild(parent, "PayoutOverlay");
        CreateChild(parent, "FaultOverlay");
        CreateChild(parent, "SettingsCanvas");
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }
}
