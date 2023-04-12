local clrLuaUserData = LuaUserData
local luaUserData = {}

luaUserData.Descriptors = {}

LuaSetup.LuaUserData = luaUserData

luaUserData.IsRegistered = clrLuaUserData.IsRegistered
luaUserData.UnregisterType = clrLuaUserData.UnregisterType
luaUserData.RegisterGenericType = clrLuaUserData.RegisterGenericType
luaUserData.UnregisterGenericType = clrLuaUserData.UnregisterGenericType
luaUserData.IsTargetType = clrLuaUserData.IsTargetType
luaUserData.TypeOf = clrLuaUserData.TypeOf
luaUserData.GetType = clrLuaUserData.GetType
luaUserData.CreateEnumTable = clrLuaUserData.CreateEnumTable
luaUserData.MakeFieldAccessible = clrLuaUserData.MakeFieldAccessible
luaUserData.MakeMethodAccessible = clrLuaUserData.MakeMethodAccessible
luaUserData.MakePropertyAccessible = clrLuaUserData.MakePropertyAccessible
luaUserData.AddMethod = clrLuaUserData.AddMethod
luaUserData.AddField = clrLuaUserData.AddField
luaUserData.RemoveMember = clrLuaUserData.RemoveMember
luaUserData.CreateUserDataFromDescriptor = clrLuaUserData.CreateUserDataFromDescriptor
luaUserData.CreateUserDataFromType = clrLuaUserData.CreateUserDataFromType

luaUserData.RegisterType = function(typeName)
	local success, result = pcall(clrLuaUserData.RegisterType, typeName)

	if not success then
		error(result, 2)
	end

    luaUserData.Descriptors[typeName] = result

    return result
end

luaUserData.RegisterTypeBarotrauma = function(typeName)
	typeName = "Barotrauma." .. typeName
	local success, result = pcall(luaUserData.RegisterType, typeName)

	if not success then
		error(result, 2)
	end

    return result
end

luaUserData.AddCallMetaTable = function (userdata)
	if userdata == nil then
		error("Attempted to add a call metatable to a nil value.", 2)
	end

	debug.setmetatable(userdata, {
		__call = function(obj, ...)
			if userdata == nil then
				error("userdata was nil.", 2)
			end

			local success, result = pcall(userdata.__new, ...)


			if not success then
				error(result, 2)
			end

			return result
		end
	})
end

luaUserData.CreateStatic = function(typeName, addCallMethod)
	local success, result = pcall(clrLuaUserData.CreateStatic, typeName)

	if not success then
		error(result, 2)
	end

	if addCallMethod then
		luaUserData.AddCallMetaTable(result)
	end

	return result
end