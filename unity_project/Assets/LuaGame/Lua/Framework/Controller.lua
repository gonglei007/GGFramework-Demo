---
--- Created by User.
--- DateTime: 2019-08-14 15:38
---
Controller = class("Controller")
local this = Controller;

function Controller:ctor()
    self.initialed = false;
    self.loaded = false;
end

function Controller:Initilize(view)
    self.view = view;
    self.initialed = true;
end

---
--- 加载View(Page)
--- TODO：utViewObject从ResourceManager来加载。
---
function Controller:LoadView(callback)
    --Debug.LogWarning("1.####->Controller:LoadView->"..tostring(self.view.name));
    self.view:Load(function()
        --self.view:OnViewLoad();
        self:OnViewLoad();
        self.loaded = true;
        if callback ~= nil then
            callback();
        end
    end);
end

function Controller:OnViewLoad()
    print("调用了父类的 OnViewLoaded");
end

function Controller:OnPageShow()
    print("调用了父类的 OnPageShow");
end

