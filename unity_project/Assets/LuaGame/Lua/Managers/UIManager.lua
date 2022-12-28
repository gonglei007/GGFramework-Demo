local bit = require("bit")
--require "Common/define"

UIManager = {};
local this = UIManager;
this.AssetSetRoot = null;

UIManager.DialogOption = {
    ["None"] = UIDialog.Option.None:ToInt(),
    ["Yes"] = UIDialog.Option.Yes:ToInt(),
    ["No"] = UIDialog.Option.No:ToInt(),
    ["Cancel"] = UIDialog.Option.Cancel:ToInt(),
    ["YesNo"] = bit.bor(UIDialog.Option.Yes:ToInt(), UIDialog.Option.No:ToInt()),
    ["YesCancel"] = bit.bor(UIDialog.Option.Yes:ToInt(), UIDialog.Option.Cancel:ToInt()),
    ["YesNoCancel"] = bit.bor(UIDialog.Option.Yes:ToInt(), UIDialog.Option.No:ToInt(), UIDialog.Option.Cancel:ToInt()),
};

function UIManager.Init()
    --by:cww 储存加载出的临时资源，使他们刚加载出时不至于暴露在游戏场景中
    this.AssetSetRoot = GameObject.New("AssetSetRoot");
    this.AssetSetRoot:SetActive(false);
--[[
    uiSystem:PopDialogX("Hello", "Good Night!", UIManager.DialogOption["YesNo"], function(option)
        print("OnDialogClose1.."..tostring(option==UIDialog.Option.Yes));
        print("OnDialogClose2.."..tostring(option==UIDialog.Option.No));
    end);
--]]
    return this;
end

function UIManager.Load()
    --uiSystem.Instance:GetLoadingUI("UILoading"):LoadFX("UISwitch_in");
    --uiSystem.Instance:GetLoadingUI("UILoading"):LoadFX("UISwitch_out");
    --uiSystem.Instance:GetLoadingUI("UILoading"):LoadFX("UISwitch_AllComplete");
end

function UIManager.LoadFloatAssetAsyncX(bundle, asset, callback)
    resourceSystem:LoadFloatAssetAsyncX(bundle, asset, function(gameObject)
        if gameObject:GetType():ToString() == "UnityEngine.GameObject" then
            gameObject.transform.parent = this.AssetSetRoot.transform;
        end
        if callback ~= nil then
            callback(gameObject);
        end
    end);
end