-- luacheck: ignore 111

--[[--
Class providing several util functions.
]]
-- @code Util
-- @pragma nostrip

local Util = {}

--- Returns a table with all items that have the given identifier.
-- @realm shared 
Util.GetItemsById = function (id) end

--- Registers a new item group, the given function will be called for every item and should return true if the item should be in the group.
-- @realm shared 
Util.RegisterItemGroup = function(groupName, func) end

--- Returns a table with all items associated with the item group.
-- @realm shared 
Util.GetItemGroup = function(groupName) end

--- Returns the Client that is currently controlling the Character, returns nil if no Client is controlling the Character.
-- @realm shared 
Util.FindClientCharacter = function (character) end