htf.on("fish_sold", function(item, seller)
  local name = seller and seller.name or "Unknown"
  htf.log(name .. " sold " .. item.name .. " for $" .. item.worth)
end)

htf.on("player_died", function(player)
  if not player or player.steam_id == "0" then return end
  local key = "deaths_" .. player.steam_id
  local count = (tonumber(htf.get_data(key, "0")) or 0) + 1
  htf.set_data(key, tostring(count))
end)

htf.on("island_loaded", function(island)
  htf.log("Island " .. island .. " finished loading")
end)

htf.button("Heal crew", function()
  for _, player in ipairs(htf.players.list()) do
    if player.steam_id ~= "0" then htf.players.heal(player.steam_id, 100) end
  end
end)

htf.button("Feed crew", function()
  for _, player in ipairs(htf.players.list()) do
    if player.steam_id ~= "0" then htf.players.feed(player.steam_id, 100) end
  end
end)

htf.command("luacrew", function()
  htf.chat("Crew: " .. #htf.players.list() .. " | Balance: $" .. htf.economy.balance())
end)

htf.command("luareturn", function(args)
  local id = args[1]
  if not id or not string.match(id, "^[1-9]%d*$") or #id > 20 then
    htf.log("Usage: /luareturn steam_id (use the string ID from htf.players.list())")
    return
  end
  local connected = false
  for _, player in ipairs(htf.players.list()) do
    if player.steam_id == id then connected = true end
  end
  if not connected then
    htf.log("No connected player with that Steam ID")
    return
  end
  local spawn = htf.world.spawn_position()
  if spawn and htf.players.teleport(id, spawn.x, spawn.y, spawn.z) then
    htf.log("Return-to-spawn teleport sent")
  else
    htf.log("Player or island not ready")
  end
end)
