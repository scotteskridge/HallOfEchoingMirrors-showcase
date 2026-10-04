using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The item Inspector and Balance Sheet show settings by section: every setting must be in
    /// exactly one, or it would be hidden without anyone noticing.
    /// </summary>
    public class ItemSectionsTests : SimulationTestBase
    {
        [Test]
        public void EveryItemSetting_IsInExactlyOneSection()
        {
            var item = ScriptableObject.CreateInstance<ResourceDefinition>();
            try
            {
                var inSections = new List<string>();
                foreach (var section in ItemSections.All)
                    inSections.AddRange(section.Fields);

                var settings = new List<string>();
                var property = new SerializedObject(item).GetIterator();
                for (bool more = property.NextVisible(true); more; more = property.NextVisible(false))
                    if (property.name != "m_Script" && property.name != "_id")
                        settings.Add(property.name);

                Assert.That(settings, Is.EquivalentTo(inSections),
                    "Add each new ResourceDefinition setting to one section in Editor/ItemSections.cs");
            }
            finally
            {
                Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void ARealItem_UsesOnlyTheSectionsItNeeds()
        {
            var satchel = MakeResource("Satchel"); // a fixture, so retuning the real one never breaks this
            satchel.addsPockets = 2;

            var used = new List<string>();
            foreach (var section in ItemSections.All)
                if (section.UsedBy != null && section.IsUsedBy(satchel))
                    used.Add(section.Title);

            Assert.That(used, Is.EqualTo(new[] { "Adds room" }), "a satchel only adds pockets");
        }
    }
}
