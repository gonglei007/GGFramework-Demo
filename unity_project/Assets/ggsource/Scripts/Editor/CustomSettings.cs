
#define USING_DOTWEENING

using UnityEngine;
using System;
using System.Collections.Generic;
using LuaInterface;
using LuaFramework;
using SimpleJson;
using UnityEditor;

using BindType = ToLuaMenu.BindType;
using System.Reflection;
using System.Text;

public static class CustomSettings
{
    public static string FrameworkPath = AppConst.FrameworkRoot;
    public static string saveDir = FrameworkPath + "/ToLua/Source/Generate/";
    public static string toluaBaseType = FrameworkPath + "/ToLua/BaseType/";
    public static string baseLuaDir = FrameworkPath + "/Tolua/Lua/";
    public static string injectionFilesPath = FrameworkPath + "/ToLua/Injection/";
    public static string luaDir = FrameworkPath + "/Lua/";
	public static string toluaLuaDir = FrameworkPath + "/ToLua/Lua";

    //导出时强制做为静态类的类型(注意customTypeList 还要添加这个类型才能导出)
    //unity 有些类作为sealed class, 其实完全等价于静态类
    public static List<Type> staticClassTypes = new List<Type>
    {        
        typeof(UnityEngine.Application),
        typeof(UnityEngine.Time),
        typeof(UnityEngine.Screen),
        typeof(UnityEngine.SleepTimeout),
        typeof(UnityEngine.Input),
        typeof(UnityEngine.Resources),
        typeof(UnityEngine.Physics),
        typeof(UnityEngine.RenderSettings),
        typeof(UnityEngine.QualitySettings),
        typeof(UnityEngine.GL),
        typeof(UnityEngine.Graphics),
    };

    //附加导出委托类型(在导出委托时, customTypeList 中牵扯的委托类型都会导出， 无需写在这里)
    public static DelegateType[] customDelegateList = 
    {        
        _DT(typeof(Action)),                
        _DT(typeof(UnityEngine.Events.UnityAction)),
        _DT(typeof(System.Predicate<int>)),
        _DT(typeof(System.Action<int>)),
        _DT(typeof(System.Comparison<int>)),
        _DT(typeof(System.Func<int, int>)),
    };

    //在这里添加你要导出注册到lua的类型列表
    public static BindType[] customTypeList =
    {
		//_GT(typeof(List<int>)),
        //------------------------为例子导出--------------------------------
        //_GT(typeof(TestEventListener)),
        //_GT(typeof(TestProtol)),
        //_GT(typeof(TestAccount)),
        //_GT(typeof(Dictionary<int, TestAccount>)).SetLibName("AccountMap"),
        //_GT(typeof(KeyValuePair<int, TestAccount>)),
        //_GT(typeof(Dictionary<int, TestAccount>.KeyCollection)),
        //_GT(typeof(Dictionary<int, TestAccount>.ValueCollection)),
        //_GT(typeof(TestExport)),
        //_GT(typeof(TestExport.Space)),
        //-------------------------------------------------------------------        

        _GT(typeof(Debugger)).SetNameSpace(null),

#if USING_DOTWEENING
        _GT(typeof(DG.Tweening.DOTween)),
        _GT(typeof(DG.Tweening.Tween)).SetBaseType(typeof(System.Object)).AddExtendType(typeof(DG.Tweening.TweenExtensions)),
        _GT(typeof(DG.Tweening.Sequence)).AddExtendType(typeof(DG.Tweening.TweenSettingsExtensions)),
        _GT(typeof(DG.Tweening.Tweener)).AddExtendType(typeof(DG.Tweening.TweenSettingsExtensions)),
        _GT(typeof(DG.Tweening.LoopType)),
        _GT(typeof(DG.Tweening.PathMode)),
        _GT(typeof(DG.Tweening.PathType)),
        _GT(typeof(DG.Tweening.RotateMode)),
        _GT(typeof(Component)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Transform)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Light)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Material)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Rigidbody)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(Camera)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(AudioSource)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        //_GT(typeof(LineRenderer)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        _GT(typeof(TrailRenderer)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
         _GT(typeof(DG.Tweening.AutoPlay)),
         _GT(typeof(DG.Tweening.AxisConstraint)),
         _GT(typeof(DG.Tweening.Ease)),
         _GT(typeof(DG.Tweening.LogBehaviour)),
         _GT(typeof(DG.Tweening.ScrambleMode)),
         _GT(typeof(DG.Tweening.TweenType)),
         _GT(typeof(DG.Tweening.DOVirtual)),
         _GT(typeof(DG.Tweening.EaseFactory)),
         _GT(typeof(DG.Tweening.TweenParams)),
         _GT(typeof(DG.Tweening.Core.ABSSequentiable)),
         _GT(typeof(DG.Tweening.Core.TweenerCore<Vector3,Vector3,DG.Tweening.Plugins.Options.VectorOptions>)),
#else
                                         
        _GT(typeof(Component)),
        _GT(typeof(Transform)),
        _GT(typeof(Material)),
        _GT(typeof(Light)),
        _GT(typeof(Rigidbody)),
        _GT(typeof(Camera)),
        _GT(typeof(AudioSource)),
        //_GT(typeof(LineRenderer))
        //_GT(typeof(TrailRenderer))
#endif
      
        _GT(typeof(Behaviour)),
        _GT(typeof(MonoBehaviour)),
        _GT(typeof(GameObject)),
        _GT(typeof(UnityEngine.Object)),
        _GT(typeof(Type)),
        _GT(typeof(TrackedReference)),
        _GT(typeof(Application)),
        _GT(typeof(RuntimePlatform)),
        _GT(typeof(Physics)),
        _GT(typeof(Collider)),
        _GT(typeof(Time)),
        _GT(typeof(Texture)),
        _GT(typeof(Texture2D)),
        _GT(typeof(Shader)),
        _GT(typeof(Renderer)),
        _GT(typeof(WWW)),
        _GT(typeof(Screen)),
        _GT(typeof(CameraClearFlags)),
        _GT(typeof(NetworkReachability)),
        _GT(typeof(AudioClip)),
        _GT(typeof(AssetBundle)),
        _GT(typeof(AsyncOperation)).SetBaseType(typeof(System.Object)),
        _GT(typeof(LightType)),
        _GT(typeof(SleepTimeout)),
        _GT(typeof(TextAsset)),
#if UNITY_5_3_OR_NEWER && !UNITY_5_6_OR_NEWER
        //_GT(typeof(UnityEngine.Experimental.Director.DirectorPlayer)),
#endif
        _GT(typeof(Animator)),
        _GT(typeof(Input)),
        _GT(typeof(KeyCode)),
        _GT(typeof(SkinnedMeshRenderer)),
        _GT(typeof(Space)),
        _GT(typeof(PlayerPrefs)),


        _GT(typeof(MeshRenderer)),
#if !UNITY_5_4_OR_NEWER
        _GT(typeof(ParticleSystem)),
        _GT(typeof(ParticleSystem.MainModule)),
        _GT(typeof(ParticleSystem.MinMaxGradient)),
        //_GT(typeof(ParticleEmitter)),
        //_GT(typeof(ParticleRenderer)),
        //_GT(typeof(ParticleAnimator)), 
#endif


		_GT(typeof(BoxCollider)),
        _GT(typeof(BoxCollider2D)),
        _GT(typeof(MeshCollider)),
        _GT(typeof(SphereCollider)),
        _GT(typeof(CharacterController)),
        _GT(typeof(CapsuleCollider)),
        _GT(typeof(Animation)),
        _GT(typeof(AnimationClip)).SetBaseType(typeof(UnityEngine.Object)),
        _GT(typeof(AnimationState)),
        _GT(typeof(AnimationBlendMode)),
        _GT(typeof(QueueMode)),
        _GT(typeof(PlayMode)),
        _GT(typeof(WrapMode)),
        _GT(typeof(QualitySettings)),
        _GT(typeof(RenderSettings)),
        //_GT(typeof(SkinWeights)),
        _GT(typeof(RenderTexture)),
        _GT(typeof(UnityEngine.Resources)),
		
		//for LuaFramework
		_GT(typeof(UILabel.Effect)),
        _GT(typeof(UILabel.Overflow)),
        _GT(typeof(UIWidget.AspectRatioSource)),
        _GT(typeof(UIWidget.Pivot)),
        _GT(typeof(Color)),
        _GT(typeof(Vector3)),
        _GT(typeof(Rect)),
        _GT(typeof(Quaternion)),
        _GT(typeof(UITweener.Method)),
        _GT(typeof(Bounds)),
        _GT(typeof(UITweener.Style)),
        _GT(typeof(Vector2)),
        _GT(typeof(Vector4)),
        _GT(typeof(UIGrid.Sorting)),
        _GT(typeof(Util)),
        _GT(typeof(WrapGrid)),
        _GT(typeof(AppConst)),
        _GT(typeof(LuaHelper)),
        _GT(typeof(ByteBuffer)),
        _GT(typeof(LuaBehaviour)),
		
		// ***********  GO Custom Class ******** //
		_GT(typeof(GameManager)),
        _GT(typeof(LuaManager)),
        _GT(typeof(PanelManager)),
        _GT(typeof(SoundManager)),
        _GT(typeof(TimerManager)),
        _GT(typeof(ThreadManager)),
        _GT(typeof(NetworkManager)),
        _GT(typeof(ResourceManager)),

        //-- System --
        _GT(typeof(Global)),
        //_GT(typeof(NativeLuaBridge)),
        _GT(typeof(GameSystem.States)),
        _GT(typeof(GameSystem)),
        _GT(typeof(StageSystem)),
        _GT(typeof(DataSystem)),
        _GT(typeof(ResourceSystem)),
        _GT(typeof(SoundSystem)),
        _GT(typeof(TextureSystem)),
        _GT(typeof(UISystem)),
        _GT(typeof(NetworkSystem)),
        _GT(typeof(VFXSystem)),
        _GT(typeof(HttpNetworkSystem)),
        _GT(typeof(TextSystem)),
        _GT(typeof(UserSystem)),

        _GT(typeof(StageSystem.StageData)),
        _GT(typeof(StageSystem.States)),
        //-- Resource --
        _GT(typeof(ResourceItem)),
        _GT(typeof(ResourceObject)),
        _GT(typeof(ResourceSystem.Tag)),
        _GT(typeof(ResourceSystem.State)),

        //-- NGUIExtend --
        _GT(typeof(InputChild)),
        _GT(typeof(InputController)),
        _GT(typeof(NGUISetRenderQueue)),
        _GT(typeof(NGUIToolsAddon)),
        _GT(typeof(TweenAlphaAddon)),
        _GT(typeof(TweenAlphaRespective)),
        _GT(typeof(TweenScaleAddon)),
        _GT(typeof(TweenValue)),
        _GT(typeof(UIAnimation)),
        _GT(typeof(UIButtonColorAddon)),
        _GT(typeof(UIButtonState)),
        _GT(typeof(UIButtonStateEx)),
        _GT(typeof(UIButtonStateEx2)),
        _GT(typeof(UIClose)),
        _GT(typeof(UIEmojiManager)),
        _GT(typeof(UIGraySprite)),
        _GT(typeof(UIRotate)),
        _GT(typeof(UIScale)),
        _GT(typeof(UIShake)),
        _GT(typeof(UIWidgetFlash)),
        _GT(typeof(UIButtonEx)),

        //-- UI --
        _GT(typeof(UISystem.UILayer)),
        _GT(typeof(UIBase)),
        _GT(typeof(UIPage)),
        _GT(typeof(UILoading)),
        _GT(typeof(UIWindow)),
        _GT(typeof(UIDialog)),
        _GT(typeof(UIDialog.Option)),
        _GT(typeof(UIComponent)),

        //-- Network --
        _GT(typeof(GGFramework.NetworkClient)),
        _GT(typeof(GGFramework.NetworkRequest)),
        _GT(typeof(NetworkSystem.ServiceInfo)),
        _GT(typeof(GGFramework.NetworkConst)),

        //-- VFX --
        _GT(typeof(VFXSystem.VFXInfo)),
        _GT(typeof(FXBase)),
        _GT(typeof(CommonFX)),
        _GT(typeof(TextFX)),

        //-- User --
        _GT(typeof(UserSystem.States)),
        _GT(typeof(UserSystem.Events)),
        _GT(typeof(UserSystem.User)),
        _GT(typeof(UserSystem.Order)),

        //--Utils
        _GT(typeof(ExtendMethod)),
        _GT(typeof(CommonTools)),
        _GT(typeof(SimpleProfile)),
        _GT(typeof(DebugGO)),
        _GT(typeof(GameDebugger)),
        _GT(typeof(ZipUtil)),
        _GT(typeof(NetworkUtil)),
        _GT(typeof(TimerSystem)),
        _GT(typeof(TimerSystem.Timer)),
        //_GT(typeof(ArrowRenderer)),
        _GT(typeof(WrapContentCallBack)),

        //Other
        _GT(typeof(PlayerProfileBase)),
		
		// ***********  Packages目录下，NGUI、AssetBundleManager目录下的类 ******** //
        _GT(typeof(MonsterLove.StateMachine.StateMachine<GameSystem.States>)),
        _GT(typeof(EnvelopContent)),
		_GT(typeof(LanguageSelection)),
		_GT(typeof(TypewriterEffect)),
		_GT(typeof(UIButton)),
		_GT(typeof(UIButtonActivate)),
		_GT(typeof(UIButtonColor)),
		_GT(typeof(UIButtonKeys)),
		_GT(typeof(UIButtonMessage)),
		_GT(typeof(UIButtonOffset)),
		_GT(typeof(UIButtonRotation)),
		_GT(typeof(UIButtonScale)),
		_GT(typeof(UICenterOnChild)),
		_GT(typeof(UICenterOnClick)),
		_GT(typeof(UIDragCamera)),
		_GT(typeof(UIDragDropContainer)),
		_GT(typeof(UIDragDropItem)),
		_GT(typeof(UIDragDropRoot)),
		_GT(typeof(UIDraggableCamera)),
		_GT(typeof(UIDragObject)),
		_GT(typeof(UIDragResize)),
		_GT(typeof(UIDragScrollView)),
		_GT(typeof(UIEventTrigger)),
		_GT(typeof(UIForwardEvents)),
		_GT(typeof(UIGrid)),
		_GT(typeof(UIImageButton)),
		_GT(typeof(UIKeyBinding)),
		_GT(typeof(UIKeyNavigation)),
		_GT(typeof(UIPlayAnimation)),
		_GT(typeof(UIPlaySound)),
		_GT(typeof(UIPlayTween)),
		_GT(typeof(UIPopupList)),
		_GT(typeof(UIProgressBar)),
		_GT(typeof(UISavedOption)),
		_GT(typeof(UIScrollBar)),
		_GT(typeof(UIScrollView)),
		_GT(typeof(UIShowControlScheme)),
		_GT(typeof(UISlider)),
		_GT(typeof(UISoundVolume)),
		_GT(typeof(UITable)),
		_GT(typeof(UIToggle)),
		_GT(typeof(UIToggledComponents)),
		_GT(typeof(UIToggledObjects)),
		_GT(typeof(UIWidgetContainer)),
		_GT(typeof(UIWrapContent)),
		_GT(typeof(ActiveAnimation)),
		_GT(typeof(BMFont)),
		_GT(typeof(BMGlyph)),
		_GT(typeof(BMSymbol)),
		_GT(typeof(ByteReader)),
		_GT(typeof(EventDelegate)),
		_GT(typeof(Localization)),
		_GT(typeof(MinMaxRangeAttribute)),
		_GT(typeof(NGUIDebug)),
		_GT(typeof(NGUIMath)),
		_GT(typeof(NGUIText)),
		_GT(typeof(NGUITools)),
		_GT(typeof(PropertyBinding)),
		_GT(typeof(PropertyReference)),
		_GT(typeof(RealTime)),
		_GT(typeof(SpringPanel)),
		_GT(typeof(UIBasicSprite)),
		_GT(typeof(UIDrawCall)),
		_GT(typeof(UIEventListener)),
		_GT(typeof(UIGeometry)),
		_GT(typeof(UIRect)),
		_GT(typeof(UISnapshotPoint)),
		_GT(typeof(UIWidget)),
		_GT(typeof(AnimatedAlpha)),
		_GT(typeof(AnimatedColor)),
		_GT(typeof(AnimatedWidget)),
		_GT(typeof(SpringPosition)),
		_GT(typeof(TweenAlpha)),
		_GT(typeof(TweenColor)),
		_GT(typeof(TweenFill)),
		_GT(typeof(TweenFOV)),
		_GT(typeof(TweenHeight)),
		_GT(typeof(TweenOrthoSize)),
		_GT(typeof(TweenPosition)),
		_GT(typeof(TweenRotation)),
		_GT(typeof(TweenScale)),
		_GT(typeof(TweenTransform)),
		_GT(typeof(TweenVolume)),
		_GT(typeof(TweenWidth)),
		_GT(typeof(UITweener)),
		_GT(typeof(UI2DSprite)),
		_GT(typeof(UI2DSpriteAnimation)),
		_GT(typeof(UIAnchor)),
		_GT(typeof(UIAtlas)),
		_GT(typeof(UICamera)),
        _GT(typeof(UICamera.MouseOrTouch)),
        _GT(typeof(UIColorPicker)),
		_GT(typeof(UIFont)),
		_GT(typeof(UIInput)),
		_GT(typeof(UIInputOnGUI)),
		_GT(typeof(UILabel)),
		_GT(typeof(UILocalize)),
		_GT(typeof(UIOrthoCamera)),
		_GT(typeof(UIPanel)),
		_GT(typeof(UIRoot)),
		_GT(typeof(UISprite)),
		_GT(typeof(UISpriteAnimation)),
		_GT(typeof(UISpriteData)),
		_GT(typeof(UIStretch)),
		_GT(typeof(UITextList)),
		_GT(typeof(UITexture)),
		_GT(typeof(UITooltip)),
		_GT(typeof(UIViewport)),
		_GT(typeof(Convert)),
		_GT(typeof(DateTime)),
		_GT(typeof(CsAsync.TaskCallback)),
		_GT(typeof(UIBasicSprite.Flip)),
		_GT(typeof(List<EventDelegate>)),
		_GT(typeof(Dictionary<int,string>)),
        _GT(typeof(Dictionary<string,string>)),
        _GT(typeof(Dictionary<string,UIAtlas>)),
		_GT(typeof(LuaInject)),
        _GT(typeof(AnimatorStateInfo)),
        _GT(typeof(CountDown)),
        _GT(typeof(AnimatorFinishCallBack)),
        _GT(typeof(PsStopCallBack)),
        _GT(typeof(SpriteRenderer)),
        _GT(typeof(Object2D)),
        _GT(typeof(Actor2D)),
        _GT(typeof(Actor)),
        _GT(typeof(UnityEngine.Random)),

        //_GT(typeof(SkeletonAnimation)),
        //_GT(typeof(SkeletonDataAsset)),
        //_GT(typeof(SkeletonRenderer)),

        _GT(typeof(JsonObject)),
		_GT(typeof(JsonArray)),
        _GT(typeof(HttpNetworkSystem.ExceptionAction)),
        //_GT(typeof(Encoding)),
    };

    public static List<Type> dynamicList = new List<Type>()
    {
        typeof(MeshRenderer),
#if !UNITY_5_4_OR_NEWER
        //typeof(ParticleEmitter),
        //typeof(ParticleRenderer),
        //typeof(ParticleAnimator),
#endif

        typeof(BoxCollider),
        typeof(BoxCollider2D),
        typeof(MeshCollider),
        typeof(SphereCollider),
        typeof(CharacterController),
        typeof(CapsuleCollider),

        typeof(Animation),
        typeof(AnimationClip),
        typeof(AnimationState),

        //typeof(SkinWeights),
        typeof(RenderTexture),
        typeof(Rigidbody),
    };

    //重载函数，相同参数个数，相同位置out参数匹配出问题时, 需要强制匹配解决
    //使用方法参见例子14
    public static List<Type> outList = new List<Type>()
    {
        
    };
        
    //ngui优化，下面的类没有派生类，可以作为sealed class
    public static List<Type> sealedList = new List<Type>()
    {
        /*typeof(Transform),
        typeof(UIRoot),
        typeof(UICamera),
        typeof(UIViewport),
        typeof(UIPanel),
        typeof(UILabel),
        typeof(UIAnchor),
        typeof(UIAtlas),
        typeof(UIFont),
        typeof(UITexture),
        typeof(UISprite),
        typeof(UIGrid),
        typeof(UITable),
        typeof(UIWrapGrid),
        typeof(UIInput),
        typeof(UIScrollView),
        typeof(UIEventListener),
        typeof(UIScrollBar),
        typeof(UICenterOnChild),
        typeof(UIScrollView),        
        typeof(UIButton),
        typeof(UITextList),
        typeof(UIPlayTween),
        typeof(UIDragScrollView),
        typeof(UISpriteAnimation),
        typeof(UIWrapContent),
        typeof(TweenWidth),
        typeof(TweenAlpha),
        typeof(TweenColor),
        typeof(TweenRotation),
        typeof(TweenPosition),
        typeof(TweenScale),
        typeof(TweenHeight),
        typeof(TypewriterEffect),
        typeof(UIToggle),
        typeof(Localization),*/
    };

    public static BindType _GT(Type t)
    {
        return new BindType(t);
    }

    public static DelegateType _DT(Type t)
    {
        return new DelegateType(t);
    }    


    [MenuItem("Lua/Attach Profiler", false, 151)]
    static void AttachProfiler()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("警告", "请在运行时执行此功能", "确定");
            return;
        }

        LuaClient.Instance.AttachProfiler();
    }

    [MenuItem("Lua/Detach Profiler", false, 152)]
    static void DetachProfiler()
    {
        if (!Application.isPlaying)
        {            
            return;
        }

        LuaClient.Instance.DetachProfiler();
    }
}
