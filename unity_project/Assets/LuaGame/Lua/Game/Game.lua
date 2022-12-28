require "3rd/pblua/login_pb"
--require "3rd/pbc/protobuf"

local lpeg = require "lpeg"

local json = require "cjson"
local util = require "3rd/cjson/util"

--local sproto = require "3rd/sproto/sproto"
--local core = require "sproto.core"
--local print_r = require "3rd/sproto/print_r"

require "Common/define"
require "Common/functions"
require("Common.List")
require("Common.Dictionary")
require("Common.Stack")
require("Common.Queue")
require("Common.LuaDelegate")
require("Common.PrefabTool")
require("Managers.DataManager")
require("Managers.ResourceManager")
require("Managers.CtrlManager")
require("Managers.UIManager")

-- require("Room.Player")
require("Game.Message.EventNumber")
require("Game.Message.MessageManager")
require("Game.Message.MessageBase")
require("Game.Message.Message")
require("Game.Message.MessageCenter")
require("Game.Message.EventNode")
-- require("Managers.ArrowManager")
-- require("Card.Model.CardConfig")
require("Managers.TextureManager")
-- require("Managers.VFXManager")
require("Managers.NetworkManager")
require("Managers.HttpNetworkManager")

--管理器--
Game = {};
local this = Game;

local gameManager;
local transform;
local gameObject;
local WWW = UnityEngine.WWW;
local currentState = null;

function Game.InitViewPanels()
    for i = 1, #PanelNames do
        require ("View/"..tostring(PanelNames[i]))
    end
end

--初始化完成，发送链接服务器信息--
function Game.OnInitOK()
    Player.Instance();
    GameLogic.Instance();
    --UIManager.Init();
    --HotfixManager.Init();
    --SoundManager.Init();
    --SceneManager.Init();

    --HeroDataManager.LoadData();

    --baseNetworkManager:CallRequest("connector.entryHandler.enterGame", "{\"test\": \"Hello\"}", "Network", "TestCallback",
    --    false, true, false, true
    --);

    -- AppConst.SocketPort = 2012;
    -- AppConst.SocketAddress = "127.0.0.1";
    -- networkMgr:SendConnect();

    --注册LuaView--
    --this.InitViewPanels();
    --测试第三方库功能--
    -- this.test_class_func();
    -- this.test_pblua_func();
    -- this.test_cjson_func();
    -- this.test_pbc_func();
    -- this.test_lpeg_func();
    -- this.test_sproto_func();
    -- coroutine.start(this.test_coroutine);

    DataManager.Init();
    --UIManager.Init();
    --CtrlManager.Init();
    -- ArrowManager.Instance():Init();
    Game.OtherInit();
    GameLogic.Init();
    VFXMode.InitPrefabNames()
    --DataManager.LoadAllData(function()
        --CtrlManager.Load();
        --CtrlManager.Create();
        --HomeCtrl.Instance():Present();
        --LobbyCtrl.Instance():Present();
        --VFXGroupManager.Instance():Init();
    --end);
    --end);
    --gameSystem:ChangeState(GameSystem.States.LoadResource);
end

function Game.OtherInit()
    ItemHelper.Instance();
end

--- 创建大厅（的实例）。要在资源加载之后。
function Game.CreateLobby()
    -- UI
end

--- 创建房间（的实例）。要在资源加载之后。
function Game.CreateRoom()
    -- 场景
    -- UI
    -- 特效
end

function Game.OnNetworkTestCall(data)
    local obj = json.decode(data);
    print(obj['menu']['id']);
end

-- Unity中状态机Change --
function Game.OnChangeState(state)
    --logWarn("==>Lua=>OnChangeState=>"..tostring(state));
end

function Game.ChangeState(state)
    gameSystem.ChangeState(state);
end

function Game.RoomExit()
    --room:GetFSM():exit("room exit");
end

-- Unity中状态机Enter --
function Game.OnEnterState(state)
    --logWarn("==>Lua=>OnEnterState=>"..tostring(state));
    local stateSwitch = {
        -- 进入登录状态，初始化场景参数。【备注】初始化完Lua脚本之后才能调用 --
        ["Loading"] = function()
            SceneManager.InitSceneParam();
        end,
    }

    --logWarn("==>stateSwitch=>"..tostring(stateSwitch));
    --logWarn("==>fun=>"..tostring(fun));
    local fun = stateSwitch[tostring(state)];
    if(fun) then
        fun();
    else                -- for case default
        --print "Case default."
    end
end

-- Unity中状态机Exit --
function Game.OnExitState(state)
    --logWarn("==>Lua=>OnExitState=>"..tostring(state));
end

--点击进入游戏按钮，进入主页面后要初始化的内容
function Game.OnEnterGame()

end

-- 从Unity引入过来的Update，作为lua中所有Update的总入口
function Game.Update(deltaTime)

end

--测试协同--
function Game.test_coroutine()
    coroutine.wait(1);

    local www = WWW("http://doc.ulua.org/readme.txt");
    coroutine.www(www);
    logWarn(www.text);
end

--测试sproto--
function Game.test_sproto_func()
    logWarn("test_sproto_func-------->>");
    local sp = sproto.parse [[
    .Person {
        name 0 : string
        id 1 : integer
        email 2 : string

        .PhoneNumber {
            number 0 : string
            type 1 : integer
        }

        phone 3 : *PhoneNumber
    }

    .AddressBook {
        person 0 : *Person(id)
        others 1 : *Person
    }
    ]]

    local ab = {
        person = {
            [10000] = {
                name = "Alice",
                id = 10000,
                phone = {
                    { number = "123456789" , type = 1 },
                    { number = "87654321" , type = 2 },
                }
            },
            [20000] = {
                name = "Bob",
                id = 20000,
                phone = {
                    { number = "01234567890" , type = 3 },
                }
            }
        },
        others = {
            {
                name = "Carol",
                id = 30000,
                phone = {
                    { number = "9876543210" },
                }
            },
        }
    }
    local code = sp:encode("AddressBook", ab)
    local addr = sp:decode("AddressBook", code)
    -- print_r(addr)
end

--测试lpeg--
function Game.test_lpeg_func()
    logWarn("test_lpeg_func-------->>");
    -- matches a word followed by end-of-string
    local p = lpeg.R"az"^1 * -1

    print(p:match("hello"))        --> 6
    print(lpeg.match(p, "hello"))  --> 6
    print(p:match("1 hello"))      --> nil
end

--测试lua类--
function Game.test_class_func()
    LuaClass:New(10, 20):test();
end

--测试pblua--
function Game.test_pblua_func()
    local login = login_pb.LoginRequest();
    login.id = 2000;
    login.name = 'game';
    login.email = 'jarjin@163.com';

    local msg = login:SerializeToString();
    LuaHelper.OnCallLuaFunc(msg, this.OnPbluaCall);
end

--pblua callback--
function Game.OnPbluaCall(data)
    local msg = login_pb.LoginRequest();
    msg:ParseFromString(data);
    print(msg);
    print(msg.id..' '..msg.name);
end

--测试pbc--
function Game.test_pbc_func()
    local path = Util.DataPath.."lua/3rd/pbc/addressbook.pb";
    log('io.open--->>>'..path);

    local addr = io.open(path, "rb")
    local buffer = addr:read "*a"
    addr:close()
    protobuf.register(buffer)

    local addressbook = {
        name = "Alice",
        id = 12345,
        phone = {
            { number = "1301234567" },
            { number = "87654321", type = "WORK" },
        }
    }
    local code = protobuf.encode("tutorial.Person", addressbook)
    LuaHelper.OnCallLuaFunc(code, this.OnPbcCall)
end

--pbc callback--
function Game.OnPbcCall(data)
    local path = Util.DataPath.."lua/3rd/pbc/addressbook.pb";

    local addr = io.open(path, "rb")
    local buffer = addr:read "*a"
    addr:close()
    protobuf.register(buffer)
    local decode = protobuf.decode("tutorial.Person" , data)

    print(decode.name)
    print(decode.id)
    for _,v in ipairs(decode.phone) do
        print("\t"..v.number, v.type)
    end
end

--测试cjson--
function Game.test_cjson_func()
    local path = Util.DataPath.."lua/3rd/cjson/example2.json";
    local text = util.file_load(path);
    LuaHelper.OnJsonCallFunc(text, this.OnJsonCall);
end

--cjson callback--
function Game.OnJsonCall(data)
    local obj = json.decode(data);
    print(obj['menu']['id']);
end

--销毁--
function Game.OnDestroy()
    --logWarn('OnDestroy--->>>');
end
