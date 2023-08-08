<Operation Class="PatchOperationSequence">
    <success>Always</success>
    <operations>
        <!-- Test operation for "Ugly" thought -->
        <li Class="PatchOperationTest">
            <xpath>Defs/ThoughtDef[defName="Ugly"]/nullifyingTraits/li[text()="OpenMinded"]</xpath>
            <success>Invert</success>
        </li>
        <li Class="PatchOperationAdd">
            <xpath>Defs/ThoughtDef[defName="Ugly"]/nullifyingTraits</xpath>
            <value>
                <li>OpenMinded</li>
                <li>AnotherTrait</li>
                <li>YetAnotherTrait</li>
                <!-- Add as many <li> elements as needed -->
            </value>
        </li>
    </operations>
</Operation>
