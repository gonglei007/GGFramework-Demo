---
--- Created by User.
--- DateTime: 2019-08-14 12:31
---
ResourceManager = {};
local this = ResourceManager;

function ResourceManager.Init()
    return this;
end

function ResourceManager.HomeInit()
    resourceSystem.Instance:Reset();
    ResourceManager.AddResourceItem("homepage", ResourceSystem.Tag.UI,0);
end

function ResourceManager.LobbyInit()
    resourceSystem.Instance:Reset();
    ResourceManager.AddResourceItem("lobbypage", ResourceSystem.Tag.UI, 0);
    --ResourceManager.AddResourceItem("noticewindow", ResourceSystem.Tag.UI, 0);
    --ResourceManager.AddResourceItem("helpwindow", ResourceSystem.Tag.UI, 0);
    --ResourceManager.AddResourceItem("uifx", ResourceSystem.Tag.UI, 0);
    --ResourceManager.AddResourceItem("shoppage", ResourceSystem.Tag.UI, 1);
end

function ResourceManager.RoomInit()
    resourceSystem.Instance:Reset();
    ResourceManager.AddResourceItem("uifx", ResourceSystem.Tag.UI, 0);
    ResourceManager.AddResourceItem("room1", ResourceSystem.Tag.UI, 0);
end

---
--- 加载系统通用资源。
---
function ResourceManager.LoadSystem()
    --TODO: GL- 加载Common UI
    --TODO: GL- 加载
end

---
--- 从配表中读取资源列表
---
--function GetResourceListFromTable(tableName)
--    local resources = DataManager.GetData(tableName);
--    local resourceList = {};
--    for k, v in pairs(resources) do
--        --Debug.LogWarning('--->>'..k..'-'..tostring(v));
--        local resourceItem = ResourceSystem.ResourceItem.New(v['name'], ResourceSystem.Tag.IntToEnum(v['tag']), v['path']);
--        table.insert(resourceList, resourceItem);
--    end
--    return resourceList;
--end

---
--- 添加资源配置。
---
function ResourceManager.AddResourceItem(bundleName, tag, priority)
    resourceSystem.Instance:AddResourceItem(ResourceItem.New(nil, tag, bundleName, priority));
end
