require "Common/functions"
require 'tolua.reflection'

Reflection = {};

local assemblyDict = {};

local BindingFlags = require 'System.Reflection.BindingFlags';
Reflection.BindingFlags = BindingFlags;

Reflection.BindingType_PrivateInstance = BindingFlags.NonPublic + BindingFlags.Instance;
Reflection.BindingType_PrivateStatic = BindingFlags.NonPublic + BindingFlags.Static;
Reflection.BindingType_Instance = BindingFlags.NonPublic + BindingFlags.Public + BindingFlags.Instance;
Reflection.BindingType_Static = BindingFlags.NonPublic + BindingFlags.Public + BindingFlags.Static;

assemblyDict["Assembly-CSharp"] = "Assembly-CSharp";
tolua.loadassembly("Assembly-CSharp");

function Reflection.LoadExtraAssembly(assemblyName)
    if (not assemblyDict[assemblyName]) then
        tolua.loadassembly('Assembly-CSharp');
        assemblyDict[assemblyName] = assemblyName;
    end
end

--特别注意！！
--传入的className或者自己用typeof()取类型时，如果是外部命名空间的类型，需要加上命名空间的前缀，比如typeof("System.String")，
--具体可以使用c#打印GetType()查看;输错的话会导致一系列奇♂怪的错误

--获取静态字段的值
function Reflection.GetStaticFieldValue(className, fieldName)
    return Reflection.GetFieldValue(className, fieldName, nil, Reflection.BindingType_Static);
end

--设置静态字段的值
function Reflection.SetStaticFieldValue(className, fieldName, newValue)
    Reflection.SetFieldValue(className, fieldName, nil, Reflection.BindingType_Static, newValue);
end

--获取实例字段的值
function Reflection.GetInstanceFieldValue(className, fieldName, instance)
    return Reflection.GetFieldValue(className, fieldName, instance , Reflection.BindingType_Instance);
end

-- 设置实例字段的值
function Reflection.SetInstanceFieldValue(className, fieldName, instance, newValue)
    Reflection.SetFieldValue(className, fieldName, instance, Reflection.BindingType_Instance, newValue);
end

--获取静态属性的值
function Reflection.GetStaticPropertyValue(className, fieldName)
    return Reflection.GetPropertyValue(className, fieldName, nil, Reflection.BindingType_Static);
end

--设置静态属性的值
function Reflection.SetStaticPropertyValue(className, fieldName, newValue)
    Reflection.SetPropertyValue(className, fieldName, nil, Reflection.BindingType_Static, newValue);
end

--获取实例属性的值
function Reflection.GetInstancePropertyValue(className, fieldName, instance)
    return Reflection.GetPropertyValue(className, fieldName, instance , Reflection.BindingType_Instance);
end

--设置实例属性的值
function Reflection.SetInstancePropertyValue(className, fieldName, instance, newValue)
    Reflection.SetPropertyValue(className, fieldName, instance, Reflection.BindingType_Instance, newValue);
end

--非静态方法调用在下面，Reflection.Call()
function Reflection.CallStatic(className, methodName, types ,...)
    Reflection.Call(className, methodName, nil, types ,...);
end

--instance:如果字段属于对象实例，instance指这个实例，否则填nil
--返回字段的值
function Reflection.GetFieldValue(className, fieldName, instance, bindingType)
    local classType = typeof(className);
    local fieldInfo = tolua.getfield(classType, fieldName, bindingType);

    local result;

    if (instance) then
        result = fieldInfo:Get(instance);
    else
        result = fieldInfo:Get();
    end

    fieldInfo:Destroy();
    fieldInfo = nil;
    return result;
end

--instance:如果字段属于对象实例，instance指这个实例，否则填nil
function Reflection.SetFieldValue(className, fieldName, instance, bindingType, newValue)
    local classType = typeof(className);
    local fieldInfo = tolua.getfield(classType, fieldName, bindingType);

    if (instance) then
        fieldInfo:Set(instance, newValue);
    else
        fieldInfo:Set(newValue);
    end

    fieldInfo:Destroy();
    fieldInfo = nil;
end

function Reflection.GetPropertyValue(className, fieldName, instance, bindingType)
    local classType = typeof(className);
    local propertyInfo = tolua.getproperty(classType, fieldName, bindingType);

    local result;

    if (instance) then
        result = propertyInfo:Get(instance, null);
    else
        result = propertyInfo:Get(null);
    end

    propertyInfo:Destroy();
    propertyInfo = nil;
    return result;
end

function Reflection.SetPropertyValue(className, fieldName, instance, bindingType, newValue)
    local classType = typeof(className);
    local propertyInfo = tolua.getproperty(classType, fieldName, bindingType);

    if (instance) then
        propertyInfo:Set(instance, newValue, null);
    else
        propertyInfo:Set(newValue, null);
    end

    propertyInfo:Destroy();
    propertyInfo = nil;
end

--嵌套类的className请自己打印GetType()查看
--instance:如果字段属于对象实例，instance指这个实例，否则填nil
--types:当方法有重载的时候，需要各参数的类型来辅助获取目标方法，方法没重载的时候填nil
function Reflection.Call(className, methodName, instance, types ,...)
    local classType = typeof(className);
    local methodInfo;

    if (types) then
        methodInfo = tolua.getmethod(classType, methodName, unpack(types));
    else
        methodInfo = tolua.getmethod(classType, methodName);
    end

    local result;

    if (instance) then
        result = {methodInfo:Call(instance, ...)};
    else
        result = {methodInfo:Call(...)};
    end

    methodInfo:Destroy();
    methodInfo = nil;

    return unpack(result);
end

function Reflection.Test()
    Debug.LogWarning("reflection start");

	--require "Common/Reflection"

	local legion = GameSystem.GetLegion(1);

	local name = Reflection.GetFieldValue("Legion", "mLegionName", legion, Reflection.BindingType_PrivateInstance);

	Debug.LogWarning(">>>> " .. name);
	Reflection.SetFieldValue("Legion", "mLegionName", legion, Reflection.BindingType_PrivateInstance, "afaew");

	local r = Reflection.Call("Legion", "Test", legion, {typeof("System.String")}, "ttttt");
    Debug.LogWarning(">>> " .. r);

    local num = Reflection.GetPropertyValue("Legion", "TestProp", legion, Reflection.BindingType_Instance);
    Reflection.SetPropertyValue("Legion", "TestProp", legion, Reflection.BindingType_Instance, 44);


    local r1, r2 = Reflection.Call("Legion", "Test22", legion, {typeof("System.String"), typeof("System.Int32"):MakeByRefType()}, "bbb", 333);
    Debug.LogWarning(">>>-- " .. r1 .. "  " .. r2);
    
    Reflection.Call("Legion", "Test44", nil, {typeof("System.Int32")}, 777);

    Reflection.Call("Legion", "Test66", legion);

end