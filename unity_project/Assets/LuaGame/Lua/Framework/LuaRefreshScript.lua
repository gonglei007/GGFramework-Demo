--lua的脚本刷新
LuaRefreshScript = {}

LuaRefreshScript.pageArr = {} --存储加载过的page对象

function LuaRefreshScript.RefreshLua(path)
	Debug.LogWarning(string.format("修改的lua脚本路径：%s",path))
	local tmpStr = string.split(path,".")
	local scriptName = tmpStr[#tmpStr]
	--local luaScript = package.loaded[path]
	local luaScript = _G[scriptName]
	if luaScript and luaScript.Instance and type(luaScript) ~= "boolean" then --防止空值报错或不是表的lua重启
		Debug.LogWarning("lua脚本有修改")
		local tmpCtrl = CtrlManager.GetCtrl(luaScript.Instance().name)
		local tmpView = not tmpCtrl and luaScript.Instance().view or nil
		package.loaded[path] = nil
		_G[scriptName] = nil
		require(path)
		--local newModel = require(path)
		local newModel = _G[scriptName]
		tmpCtrl = tmpCtrl and tmpCtrl or newModel.Instance()
		tmpView = tmpView and tmpView or newModel.Instance()
		CtrlManager.CreateController(tmpCtrl,tmpView)
		local page =  LuaRefreshScript.pageArr[newModel.__cname]
		if page then
			tmpView.uiPage = page
			tmpView.gameObject = page.gameObject
			tmpView.transform = page.transform
		end
		if tmpCtrl.view and page then
			tmpCtrl.view:Load()
		end
	else
		Debug.LogWarning("这个脚本没有加载")
	end
end

return LuaRefreshScript