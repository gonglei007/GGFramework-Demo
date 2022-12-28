require "Common/define"

require("Framework.View")
require("Framework.PageView")
require("Framework.WindowView")
require("Framework.Controller")
require("Framework.PageController")
require("Framework.WindowController")

--
-- Ctroller管理器。要在UIManager的资源加载之后使用。
--
CtrlManager = {};
local this = CtrlManager;
local ctrlList = {};	--控制器列表--
local activeController = nil;  --激活的控制器

function CtrlManager.Init()
    resourceSystem.onResourceLoadedX = function(resourceItem)
        CtrlManager.OnResourceLoaded(resourceItem);
    end;
end

function CtrlManager.OnResourceLoaded(resourceItem)
    --Debug.LogWarning("OnResourceLoaded========>>>>>"..resourceItem.name);
    local ctrl = ctrlList[resourceItem.name];
    if ctrl ~= nil then
        --Debug.LogWarning("ctrl========>>>>>"..tostring(ctrl.loaded).." | "..ctrl.view.name);
        if ctrl.loaded == nil or ctrl.loaded == false then
            ctrl:LoadView();
        end
    end
end

function CtrlManager.Reset()
    for k,v in pairs(ctrlList) do
        ctrlList[k] = nil;
    end
    ctrlList = {};
end

---
--- 加载View。从UIManager获取utPageObjects。
---
function CtrlManager.Load() 
    --Debug.Log("[Lua]CtrlManager.Load | 开始加载Controllers->");
    --for k,v in pairs(ctrlList) do
    --    if v.loaded == nil or v.loaded == false then
    --        --v:LoadView();
    --    end
    --end
end

--- 创建一个Controller
function CtrlManager.CreateController(controller, view)
    controller:Initilize(view);
    --print(view.name)
    ctrlList[view.name] = controller;
end

--添加控制器--
function CtrlManager.AddCtrl(ctrlName, ctrlObj)
    print(ctrlName);
    ctrlList[ctrlName] = ctrlObj;
end

--获取控制器--
function CtrlManager.GetCtrl(ctrlName)
    return ctrlList[ctrlName];
end

--移除控制器--
function CtrlManager.RemoveCtrl(ctrlName)
    ctrlList[ctrlName] = nil;
end

--关闭控制器--
function CtrlManager.Close()
end

function CtrlManager.SetActiveController(controller)
    activeController = controller;
end

function CtrlManager.GetActiveController()
    return activeController;
end