require "Common.Class"

--SceneNames = {
--	--保持这两个场景在最前，否则有不明原因导致unity挂掉，待查。。。
--
--	"MainUIScene",
--}

--CtrlNames = {
--	--Catalog = "CatalogCtrl",
--	--Viewer = "ViewerCtrl",
--}
--
--PanelNames = {
--	--"CatalogPanel",
--	--"ViewerPanel",
--}

--协议类型--
ProtocalType = {
	BINARY = 0,
	PB_LUA = 1,
	PBC = 2,
	SPROTO = 3,
}
--当前使用的协议类型--
TestProtoType = ProtocalType.BINARY;

Util = LuaFramework.Util;
AppConst = LuaFramework.AppConst;
LuaHelper = LuaFramework.LuaHelper;
ByteBuffer = LuaFramework.ByteBuffer;

--sceneManager = LuaHelper.GetSceneManager();
--resMgr = LuaHelper.GetResManager();
--panelMgr = LuaHelper.GetPanelManager();
--soundMgr = LuaHelper.GetSoundManager();
--networkMgr = LuaHelper.GetNetManager();
--baseNetworkManager = LuaHelper.GetBaseNetworkManager();
soundSystem = LuaHelper.GetSoundSystem();
--subGameSystem = LuaHelper.GetSubGameSystem();
stageSystem = LuaHelper.GetStageSystem();
resourceSystem = LuaHelper.GetResourceSystem();
uiSystem = LuaHelper.GetUISystem();
networkSystem = LuaHelper.GetNetworkSystem();
vfxSystem = LuaHelper.GetVFXSystem();
userSystem = LuaHelper.GetUserSystem();

WWW = UnityEngine.WWW;
GameObject = UnityEngine.GameObject;
