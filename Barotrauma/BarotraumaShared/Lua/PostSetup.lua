if CSActive then
    return
end

local function IsAllowed(typeName)
    if string.startsWith(typeName, "Barotrauma.Lua") or string.startsWith(typeName, "Barotrauma.Cs") or string.startsWith(typeName, "Barotrauma.LuaCs") then
        return false
    end

    if string.startsWith(typeName, "System.Collections") then return true end

    if string.startsWith(typeName, "Microsoft.Xna") then return true end

    if string.startsWith(typeName, "Barotrauma.IO") then return false end

    if string.startsWith(typeName, "Barotrauma.ToolBox") then return false end
    if string.startsWith(typeName, "Barotrauma.SaveUtil") then return false end

    if string.startsWith(typeName, "Barotrauma.") then return true end

    return false
end

local function CanBeReRegistered(typeName)
    if string.startsWith(typeName, "Barotrauma.Lua") or string.startsWith(typeName, "Barotrauma.Cs") or string.startsWith(typeName, "Barotrauma.LuaCs") then
        return false
    end

    return true
end

local originalRegisterType = LuaUserData.RegisterType
LuaUserData.RegisterType = function (typeName)
    if not (CanBeReRegistered(typeName) and LuaUserData.IsRegistered(typeName)) and not IsAllowed(typeName) then
        error("Couldn't register type " .. typeName .. ".", 2)
    end

    local success, result = pcall(originalRegisterType, typeName)

	if not success then
		error(result, 2)
	end

    return result
end

local originalRegisterGenericType = LuaUserData.RegisterType
LuaUserData.RegisterGenericType = function (typeName, ...)
    if not (CanBeReRegistered(typeName) and LuaUserData.IsRegistered(typeName)) and not IsAllowed(typeName) then
        error("Couldn't register generic type " .. typeName .. ".", 2)
    end

    local success, result = pcall(originalRegisterGenericType, typeName, ...)

	if not success then
		error(result, 2)
	end

    return result
end

local originalCreateStatic = LuaUserData.CreateStatic
LuaUserData.CreateStatic = function (typeName, addCallMethod)
    if not (CanBeReRegistered(typeName) and LuaUserData.IsRegistered(typeName)) and not IsAllowed(typeName) then
        error("Couldn't create static type " .. typeName .. ".", 2)
    end

    local success, result = pcall(originalCreateStatic, typeName, addCallMethod)

	if not success then
		error(result, 2)
	end

    return result
end