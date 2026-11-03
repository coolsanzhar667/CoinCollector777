using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

// Reproducible authoring tool: every scene is saved with editable objects and references.
[InitializeOnLoad]
public static class ProjectBuilder
{
    static readonly Color Ink=new Color(.035f,.075f,.12f), Teal=new Color(.35f,.88f,.81f), White=new Color(.88f,.96f,.97f), Muted=new Color(.52f,.68f,.75f), Gold=new Color(1,.79f,.39f);
    static TMP_FontAsset regular,bold;
    static GameObject coinPrefab;
    static Transform canvas;
    static ProjectBuilder() { EditorApplication.delayCall+=Auto; }
    static void Auto() { if(File.Exists("BUILD_REQUEST") && !EditorApplication.isCompiling && !EditorApplication.isPlaying) { File.Delete("BUILD_REQUEST"); try { BuildAll(); } catch(Exception e) { File.WriteAllText("BUILD_ERROR.txt",e.ToString()); Debug.LogException(e); } } }
    [MenuItem("Coin Collector/Rebuild scenes and Windows game")]
    public static void BuildAll()
    {
        Configure(); ImportArt(); MakeFonts(); MakeCoin();
        MakeMenu(); for(int i=1;i<=3;i++) MakeLevel(i); MakeLab();
        string[] names={"MainMenu","Game","Level2","Level3","Module2"};
        EditorBuildSettings.scenes=names.Select(n=>new EditorBuildSettingsScene("Assets/Scenes/"+n+".unity",true)).ToArray();
        AssetDatabase.SaveAssets(); Validate();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        string outDir=Path.GetFullPath("../Windows");Directory.CreateDirectory(outDir);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(), locationPathName=outDir+"/CoinCollector.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
        File.WriteAllText("BUILD_RESULT.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nSize: "+report.summary.totalSize);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed");
    }
    static void Configure()
    {
        PlayerSettings.companyName="OrbitLab";PlayerSettings.productName="CoinCollector";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.colorSpace=ColorSpace.Gamma;
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=settings.FindProperty("activeInputHandler"); if(input!=null) input.intValue=0; settings.ApplyModifiedPropertiesWithoutUndo();
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var arr=tags.FindProperty("tags");foreach(string name in new[]{"Ground","Coin","Enemy"}) { bool found=false;for(int j=0;j<arr.arraySize;j++) if(arr.GetArrayElementAtIndex(j).stringValue==name) found=true; if(!found){arr.InsertArrayElementAtIndex(arr.arraySize);arr.GetArrayElementAtIndex(arr.arraySize-1).stringValue=name;} }
        tags.FindProperty("layers").GetArrayElementAtIndex(6).stringValue="Ground";tags.ApplyModifiedPropertiesWithoutUndo();
        Physics2D.gravity=new Vector2(0,-9.81f);Time.fixedDeltaTime=.02f;
        EditorSettings.serializationMode=SerializationMode.ForceText;
    }
    static void ImportArt()
    {
        foreach(string path in Directory.GetFiles("Assets/Art","*.png")) { var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;t.SaveAndReimport(); }
    }
    static TMP_FontAsset FontAsset(string source,string target)
    {
        var f=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(target); if(f) return f;
        f=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source),48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        f.name=Path.GetFileNameWithoutExtension(target);AssetDatabase.CreateAsset(f,target);
        foreach(var tex in f.atlasTextures) {tex.name=f.name+" Atlas";AssetDatabase.AddObjectToAsset(tex,f);}
        f.material.name=f.name+" Material";AssetDatabase.AddObjectToAsset(f.material,f);
        f.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?/—→← +()-ӘәҒғҚқҢңӨөҰұҮүҺһІіАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя");
        EditorUtility.SetDirty(f);return f;
    }
    static void MakeFonts() {regular=FontAsset("Assets/Fonts/Arial.ttf","Assets/Fonts/Interface.asset");bold=FontAsset("Assets/Fonts/ArialBold.ttf","Assets/Fonts/InterfaceBold.asset");AssetDatabase.SaveAssets();}
    static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/"+name+".png");
    static GameObject Art(string name,string sprite,Vector2 pos,Vector2 size,int order=0)
    {
        var o=new GameObject(name);o.transform.position=new Vector3(pos.x,pos.y,0);var r=o.AddComponent<SpriteRenderer>();r.sprite=Sprite(sprite);r.sortingOrder=order;
        o.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1);return o;
    }
    static void MakeCoin()
    {
        var clip=new AnimationClip {name="CoinPulse",frameRate=60};
        foreach(string a in new[]{"x","y"}) clip.SetCurve("",typeof(Transform),"m_LocalScale."+a,new AnimationCurve(new Keyframe(0,1),new Keyframe(.5f,1.3f),new Keyframe(1,1)));
        var loop=AnimationUtility.GetAnimationClipSettings(clip);loop.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,loop);
        string cp="Assets/Animations/CoinPulse.anim";if(AssetDatabase.LoadAssetAtPath<AnimationClip>(cp))AssetDatabase.DeleteAsset(cp);AssetDatabase.CreateAsset(clip,cp);
        string ac="Assets/Animations/CoinPulse.controller";if(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ac))AssetDatabase.DeleteAsset(ac);
        var ctrl=AnimatorController.CreateAnimatorControllerAtPath(ac);ctrl.AddMotion(clip);
        var coin=Art("Coin","Coin",Vector2.zero,new Vector2(.64f,.64f),4);coin.tag="Coin";coin.AddComponent<CircleCollider2D>().isTrigger=true;coin.GetComponent<CircleCollider2D>().radius=.26f;
        coin.AddComponent<Animator>().runtimeAnimatorController=ctrl;coinPrefab=PrefabUtility.SaveAsPrefabAsset(coin,"Assets/Prefabs/Coin.prefab");UnityEngine.Object.DestroyImmediate(coin);
    }
    static void NewScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cam=new GameObject("Main Camera");cam.tag="MainCamera";cam.transform.position=new Vector3(0,0,-10);var c=cam.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=7;c.backgroundColor=Ink;c.clearFlags=CameraClearFlags.SolidColor;cam.AddComponent<AudioListener>();
        var audio=cam.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Orbit.wav");audio.loop=true;audio.playOnAwake=true;audio.volume=.3f;
        Art("Nebula & mountains","Nebula",Vector2.zero,new Vector2(24.89f,14),-20);
        var cv=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));cv.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=cv.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;canvas=cv.transform;
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
    }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {var o=new GameObject(name,typeof(RectTransform));var r=o.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
    static Image Box(string name,Transform p,float x,float y,float w,float h,Color color)
    {var r=Rect(name,p,x,y,w,h);var img=r.gameObject.AddComponent<Image>();img.color=color;return img;}
    static TMP_Text Text(string name,Transform p,string text,float x,float y,float w,float h,int size,Color color,bool heavy=false,TextAlignmentOptions align=TextAlignmentOptions.Left)
    {var r=Rect(name,p,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=heavy?bold:regular;t.text=text;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
    static Button Button(string name,Transform p,string label,float x,float y,float w,UnityAction action,bool primary=false)
    {var i=Box(name,p,x,y,w,52,primary?Teal:new Color(.10f,.19f,.26f));var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;var colors=b.colors;colors.highlightedColor=new Color(.8f,1,1);colors.pressedColor=new Color(.5f,.8f,.8f);b.colors=colors;UnityEventTools.AddPersistentListener(b.onClick,action);Text("Label",i.transform,label,0,0,w-20,45,19,primary?Ink:White,true,TextAlignmentOptions.Center);return b;}
    static void Save(string name) {EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/"+name+".unity");}
    static void MakeMenu()
    {
        NewScene();var m=new GameObject("MenuManager").AddComponent<MenuManager>();
        Box("Left veil",canvas,-350,0,580,720,new Color(.025f,.055f,.09f,.72f));
        Text("Brand",canvas,"ORBIT / 01",-360,285,420,40,18,Teal,true);
        Text("Title",canvas,"Тиын\nжинаушы",-325,155,490,180,66,White,true);
        Text("Subtitle",canvas,"Үш әлем. Бір саяхат.\nБарлық тиынды жинап, рекорд орнат.",-330,25,480,75,23,Muted);
        Button("PlayButton",canvas,"Ойнау   →",-410,-80,320,m.PlayGame,true);
        Button("LabButton",canvas,"C# зертханасы",-490,-150,160,m.OpenLab);
        Button("QuitButton",canvas,"Шығу",-320,-150,150,m.QuitGame);
        m.recordText=Text("RecordText",canvas,"Рекорд жоқ",-330,-220,480,40,20,Gold);
        Text("Controls",canvas,"A / D  —  жүру     SPACE  —  секіру\nESC  —  пауза",-330,-290,480,60,16,Muted);
        Art("Hero island","Platform",new Vector2(5,-2.2f),new Vector2(7,1.05f),0);
        Art("Explorer illustration","Explorer",new Vector2(5,-.2f),new Vector2(2.5f,2.7f),3);
        for(int i=0;i<3;i++){var coin=(GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);coin.transform.position=new Vector3(3.3f+i*1.8f,2.3f+Mathf.Sin(i)*.4f,0);}
        Text("MissionTag",canvas,"01 / 03     •     ЖҰЛДЫЗДАРҒА ЖОЛ",290,-205,470,36,16,Teal,true,TextAlignmentOptions.Center);
        Text("MissionInfo",canvas,"05  /  08  /  10\nӘр деңгейде жаңа сынақ",290,-267,470,70,22,White,false,TextAlignmentOptions.Center);
        Button("SoundButton",canvas,"Дыбыс",525,300,130,m.ToggleSound);Save("MainMenu");
    }
    static void Platform(float x,float y,float width)
    {var g=Art("Ground","Platform",new Vector2(x,y),new Vector2(width,.5f),0);g.tag="Ground";g.layer=6;var bc=g.AddComponent<BoxCollider2D>();bc.size=new Vector2(3.2f,.5f/.5f*.58f);}
    static void Coin(float x,float y) {var c=(GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);c.transform.position=new Vector3(x,y,0);}
    static void Spike(float x,float y)
    {var s=Art("Spike","Spike",new Vector2(x,y),new Vector2(.95f,.68f),2);s.tag="Enemy";var bc=s.AddComponent<BoxCollider2D>();bc.size=new Vector2(.8f,.6f);}
    static GameObject Panel(string name,string eyebrow,string title,string subtitle,out Transform card)
    {var overlay=Box(name,canvas,0,0,1600,1100,new Color(.015f,.035f,.065f,.88f));card=Box("Card",overlay.transform,0,0,700,450,new Color(.055f,.11f,.17f)).transform;Box("Accent",card,0,224,700,3,Teal);Text("Eyebrow",card,eyebrow,0,165,630,35,16,Teal,true,TextAlignmentOptions.Center);Text("Title",card,title,0,88,650,90,48,White,true,TextAlignmentOptions.Center);Text("Subtitle",card,subtitle,0,12,630,70,20,Muted,false,TextAlignmentOptions.Center);return overlay.gameObject;}
    static void MakeLevel(int level)
    {
        NewScene();var manager=new GameObject("GameManager").AddComponent<MenuManager>();
        var hero=Art("Player","Explorer",new Vector2(-10,-3.5f),new Vector2(.82f,.89f),5);hero.tag="Player";
        var body=hero.AddComponent<Rigidbody2D>();body.gravityScale=2;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var collider=hero.AddComponent<BoxCollider2D>();collider.size=new Vector2(.63f,.94f);collider.offset=new Vector2(0,-.015f);
        var mat=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Player.physicsMaterial2D");if(!mat){mat=new PhysicsMaterial2D("Player") {friction=0,bounciness=0};AssetDatabase.CreateAsset(mat,"Assets/Player.physicsMaterial2D");} collider.sharedMaterial=mat;
        hero.AddComponent<AudioSource>();var p=hero.AddComponent<Player>();manager.player=p;p.totalCoins=level==1?5:level==2?8:10;
        p.coinSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Coin.wav");p.jumpSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Jump.wav");p.hurtSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hurt.wav");
        Platform(0,-4.6f,24);for(int i=0;i<5;i++){float y=-2.9f+i*(level==3?1.45f:1.35f);Platform(-7+i*4,y,level==1?3.3f:2.8f);Coin(-7+i*4,y+.9f);}
        if(level>=2){Coin(-8.7f,-3.6f);Coin(0,-3.6f);Coin(8.7f,-3.6f);}
        if(level==3){Coin(-3.8f,-.35f);Coin(4.2f,2.55f);}
        Spike(-4.5f,-4.02f);if(level>=2){Spike(3,-4.02f);Spike(7,-4.02f);}if(level==3){Spike(-2.0f,-1.0f);Spike(6,1.9f);}
        foreach(float x in new[]{-12.2f,12.2f}){var wall=new GameObject("Boundary");wall.transform.position=new Vector3(x,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.4f,20);}
        Box("HUD",canvas,0,300,1180,75,new Color(.03f,.08f,.13f,.94f));
        p.scoreText=Text("ScoreText",canvas,"Ұпай: 0",-420,306,290,36,26,Gold,true);
        Text("LevelTitle",canvas,"0"+level+" / 03   ·   "+new[]{"","БАСТАУ","ОРБИТА","ШЫҢ"}[level],0,316,330,32,18,Teal,true,TextAlignmentOptions.Center);
        manager.timerText=Text("TimerText",canvas,"0.0 с",0,285,200,30,18,Muted,false,TextAlignmentOptions.Center);
        p.healthText=Text("HealthText",canvas,"Өмір: 100",425,318,260,32,21,White,true);
        var sliderGO=Rect("HealthSlider",canvas,425,287,260,10);var back=sliderGO.gameObject.AddComponent<Image>();back.color=new Color(.15f,.24f,.3f);var slider=sliderGO.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=100;slider.value=100;slider.wholeNumbers=true;slider.interactable=false;
        var fill=Box("Fill",sliderGO,0,0,260,10,Teal);slider.fillRect=fill.rectTransform;slider.targetGraphic=fill;p.healthSlider=slider;
        Text("Controls",canvas,"A / D  —  жүру     SPACE  —  секіру     ESC  —  пауза",-225,-319,720,35,16,Muted);
        Text("Goal",canvas,"Барлық тиынды жина",410,-319,350,35,17,Gold,false,TextAlignmentOptions.Right);
        Transform card;p.winPanel=Panel("WinPanel","MISSION COMPLETE",level==3?"Барлық деңгей өтті!":"Жеңіс!","Барлық тиын жиналды. Тамаша саяхат!",out card);
        p.resultText=Text("ResultText",card,"Уақыт: 0.0 с / Рекорд: 0.0 с",0,-48,640,50,22,Gold,true,TextAlignmentOptions.Center);
        if(level<3)Button("NextButton",card,"Келесі деңгей   →",0,-112,400,manager.NextLevel,true);
        Button("RestartButton",card,"Қайта бастау",-115,-177,220,manager.RestartGame,level==3);Button("MenuButton",card,"Мәзір",120,-177,220,manager.ToMenu);p.winPanel.SetActive(false);
        p.gameOverPanel=Panel("GameOverPanel","TRY AGAIN","Ойын аяқталды!","Шиптардан сақ бол. Қайта байқап көр!",out card);
        Button("RestartButton",card,"Қайта бастау",0,-85,400,manager.RestartGame,true);Button("MenuButton",card,"Мәзір",0,-155,400,manager.ToMenu);p.gameOverPanel.SetActive(false);
        var pause=new GameObject("PauseManager").AddComponent<PauseManager>();pause.player=p;pause.pausePanel=Panel("PausePanel","TAKE A BREATH","Пауза","Уақыт тоқтады. Саяхат сені күтеді.",out card);
        Button("ResumeButton",card,"Жалғастыру",0,-85,400,pause.Resume,true);Button("MenuButton",card,"Мәзір",0,-155,400,manager.ToMenu);pause.pausePanel.SetActive(false);
        Save(level==1?"Game":"Level"+level);
    }
    static void MakeLab()
    {
        NewScene();var m=new GameObject("MenuManager").AddComponent<MenuManager>();
        Text("Eyebrow",canvas,"MODULE 02 / C#",-320,294,550,40,18,Teal,true);Text("Title",canvas,"C# зертханасы",-285,235,620,80,46,White,true);
        string[] titles={"01  Цикл","02  Массив","03  Әдіс","04  Өмір"};
        string[] contents={"1 → 2 → 3 → 4 → 5\n5 → 4 → 3 → 2 → 1","Сәлем, Айдар!\nМадина, Ерлан,\nАружан, Данияр","AddScore(10, 5, 20, 100)\n10 → 15 → 35 → 135","TakeDamage(30, 50, 40)\n70 → 20 → -20\nОйын аяқталды!"};
        for(int i=0;i<4;i++){float x=-450+i*300;var box=Box("LessonCard"+i,canvas,x,65,280,210,new Color(.04f,.11f,.17f,.95f));Text("Title",box.transform,titles[i],0,65,244,45,24,Teal,true);Text("Result",box.transform,contents[i],0,-20,244,115,19,White);}
        var tasks=new GameObject("Exercises");tasks.AddComponent<Task1>();tasks.AddComponent<Task2>();tasks.AddComponent<Task3>();tasks.AddComponent<HealthLesson>();
        Text("MoverInfo",canvas,"05  Қозғалыс   /   A-D немесе ← →",-250,-115,700,45,25,Gold,true);Text("SpeedInfo",canvas,"1 = 2 м/с     2 = 5 м/с     3 = 10 м/с     SPACE → «Секіру!» (Console)",-150,-165,900,40,19,Muted);
        var square=Art("Square","Explorer",new Vector2(0,-4.4f),new Vector2(.9f,.98f),5);square.AddComponent<Mover>();
        Button("MenuButton",canvas,"Мәзір",505,285,160,m.ToMenu);Text("Footer",canvas,"Нәтижелер Unity Console терезесіне де шығарылады.",0,-315,1000,35,17,Muted,false,TextAlignmentOptions.Center);Save("Module2");
    }
    [MenuItem("Coin Collector/Validate scenes")]
    public static void Validate()
    {
        string report="Scene validation\n";
        for(int i=1;i<=3;i++) {string name=i==1?"Game":"Level"+i;EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var p=UnityEngine.Object.FindFirstObjectByType<Player>();int coins=GameObject.FindGameObjectsWithTag("Coin").Length;
            if(coins!=p.totalCoins || !p.healthSlider || !p.resultText || !p.coinSound || !p.winPanel || !p.gameOverPanel) throw new Exception("Invalid references/count: "+name);
            if((p.winPanel.GetComponentsInChildren<Button>(true).Any(b=>b.name=="NextButton")) != (i<3))throw new Exception("Next button invalid");
            report+=name+": "+coins+" coins; all references assigned; next-level boundary OK\n";
        }
        File.WriteAllText("VALIDATION.txt",report);
    }
}

