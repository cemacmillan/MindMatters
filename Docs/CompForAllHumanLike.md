<Operation Class="PatchOperationFindMod">
  <mods>
    <li>Humanoid Alien Races</li>
  </mods>
  <nomatch Class="PatchOperationConditional">
    <xpath>Defs/ThingDef[defName="Human"]/comps</xpath>
    <nomatch Class="PatchOperationAdd">
    <xpath>Defs/ThingDef[defName="Human"]</xpath>
      <value>
        <comps />
      </value>
    </nomatch>
  </nomatch>
  <nomatch Class="PatchOperationConditional">
    <xpath>Defs/AlienRace.ThingDef_AlienRace[not(@Abstract) and not(comps)]</xpath>
    <match Class="PatchOperationAdd">
      <xpath>AlienRace.ThingDef_AlienRace[not(@Abstract) and not(comps)]</xpath>
      <value>
        <comps />
      </value>
    </match>
  </nomatch>
</Operation>

<Operation Class="PatchOperationFindMod">
  <mods>
    <li>Humanoid Alien Races</li>
  </mods>
  <nomatch Class="PatchOperationAdd">
    <xpath>Defs/ThingDef[defName="Human"]/comps</xpath>
    <value>
      <li Class="MyNamespace.MyComp" />
    </value>
  </nomatch>
  <match Class="PatchOperation">
    <xpath>Defs/AlienRace.ThingDef_AlienRace[not(@Abstract)]/comps</xpath>
    <value>
      <li Class="MyNamespace.MyComp" />
    </value>
  </match>
</Operation>