htf.on("boat_motor_changed", function(previous, current)
  htf.log("Boat motor changed: " .. previous .. " -> " .. current)
end)

htf.on("npc_quest_progressed", function(npc, quest, progress, kind)
  htf.log("NPC " .. npc .. " quest " .. quest .. ": " .. progress .. " (" .. kind .. ")")
end)

htf.button("Show boat status", function()
  local boat = htf.boat.info()
  if not boat then return end
  htf.chat("Boat motor: " .. boat.motor .. " | Radar: " .. tostring(boat.radar_unlocked))
end)

htf.button("Save world", function()
  if htf.server.save() then
    htf.log("World save requested. Check the game log for disk errors.")
  else
    htf.log("World not ready or save cooldown active")
  end
end)

htf.command("luaspawn", function(args)
  local id = tonumber(args[1])
  if not id or id ~= math.floor(id) or id < 0 or id > 255 then
    htf.log("Usage: /luaspawn definition_id (0-255)")
    return
  end
  local player = htf.players.local_player()
  local definition = htf.catalog.item(id)
  if not player or not definition or not definition.spawn_allowed then
    htf.log("Player/item unavailable or this prefab is protected")
    return
  end
  local p = player.position
  local item = htf.items.spawn(id, p.x + 3, p.y + 1, p.z)
  htf.log(item and ("Spawned " .. item.name .. " [" .. item.network_id .. "]") or "Spawn not ready or limit reached")
end)

htf.command("luacatalog", function()
  for _, item in ipairs(htf.catalog.items()) do
    htf.log(item.id .. ": " .. item.name .. " | Spawn allowed: " .. tostring(item.spawn_allowed))
  end
end)
