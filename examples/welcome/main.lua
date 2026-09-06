local catches = tonumber(htf.get_data("catches", "0"))

htf.on("fish_hooked", function(fish_name)
  catches = catches + 1
  htf.set_data("catches", tostring(catches))
  htf.chat("Crew catch #" .. catches .. ": " .. fish_name)
end)

htf.on("boss_killed", function(boss_name)
  htf.chat("Boss defeated: " .. boss_name)
end)

htf.command("bonus", function(args)
  htf.money(25)
  htf.chat("The crew received $25.")
end)

htf.log("Welcome Example loaded. Use /bonus as host.")

htf.button("Show catch count", function()
  htf.chat("Crew catches: " .. catches)
end)
