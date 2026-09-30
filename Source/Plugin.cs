using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace ValheimItemCatalog
{
    [BepInPlugin("warpalicious.ValheimItemCatalog", "Valheim Item Catalog", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<string> outputPath = null!;
        private ConfigEntry<bool> exportOnClients = null!;

        private void Awake()
        {
            outputPath = Config.Bind("Export", "OutputPath", "item-catalog.json", "Absolute path or path relative to BepInEx. Replaced after a complete export.");
            exportOnClients = Config.Bind("Export", "ExportOnClients", false, "Enable only for local catalogue validation. Servers export automatically.");
            StartCoroutine(ExportWhenReady());
        }

        private IEnumerator ExportWhenReady()
        {
            while (ZNet.instance == null || ZNetScene.instance == null || ObjectDB.instance == null || Game.instance == null)
                yield return new WaitForSeconds(1);
            if (!ZNet.instance.IsDedicated() && !exportOnClients.Value) yield break;
            int previousCount = -1;
            int stableChecks = 0;
            while (stableChecks < 5)
            {
                yield return new WaitForSeconds(1);
                int count = ObjectDB.instance.m_items.Count;
                stableChecks = count > 0 && count == previousCount ? stableChecks + 1 : 0;
                previousCount = count;
            }
            try { Export(); }
            catch (Exception exception) { Logger.LogError("Item catalogue export failed: " + exception); }
        }

        private void Export()
        {
            Catalog catalog = new Catalog
            {
                exported_at = DateTime.UtcNow.ToString("o"),
                game_version = Version.GetVersionString(),
                language = Localization.instance.GetSelectedLanguage(),
                dedicated_server = ZNet.instance.IsDedicated(),
                mods = Chainloader.PluginInfos.Values.Select(info => new Mod
                {
                    guid = info.Metadata.GUID, name = info.Metadata.Name, version = info.Metadata.Version.ToString()
                }).OrderBy(mod => mod.guid, StringComparer.Ordinal).ToList()
            };
            // Use the same ObjectDB lookup that validates Server Chest deliveries.
            foreach (GameObject prefab in ObjectDB.instance.m_items.OrderBy(item => item.name, StringComparer.Ordinal))
            {
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || ObjectDB.instance.GetItemPrefab(prefab.name) != prefab) continue;
                ItemDrop.ItemData.SharedData data = drop.m_itemData.m_shared;
                string displayName = Localization.instance.Localize(data.m_name);
                catalog.items.Add(new Item
                {
                    prefab = prefab.name, display_name = displayName, localization_key = data.m_name,
                    max_stack_size = data.m_maxStackSize, max_quality = data.m_maxQuality,
                    item_type = data.m_itemType.ToString(),
                    localization_resolved = !displayName.Contains("[" + data.m_name.TrimStart('$') + "]") && !displayName.StartsWith("$")
                });
            }
            if (catalog.items.Count == 0) throw new InvalidOperationException("No items were registered.");
            string path = Path.IsPathRooted(outputPath.Value) ? outputPath.Value : Path.Combine(Paths.BepInExRootPath, outputPath.Value);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            string temporary = path + ".tmp";
            try
            {
                using (FileStream stream = File.Create(temporary))
                    new DataContractJsonSerializer(typeof(Catalog)).WriteObject(stream, catalog);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            Logger.LogInfo("Exported " + catalog.items.Count + " items in " + catalog.language + " to " + path + "; unresolved names=" + catalog.items.Count(item => !item.localization_resolved));
        }
    }

    [DataContract]
    public sealed class Catalog
    {
        [DataMember] public int schema_version = 1;
        [DataMember] public string exported_at = "";
        [DataMember] public string game_version = "";
        [DataMember] public string language = "";
        [DataMember] public bool dedicated_server;
        [DataMember] public List<Mod> mods = new List<Mod>();
        [DataMember] public List<Item> items = new List<Item>();
    }
    [DataContract]
    public sealed class Mod
    {
        [DataMember] public string guid = "";
        [DataMember] public string name = "";
        [DataMember] public string version = "";
    }
    [DataContract]
    public sealed class Item
    {
        [DataMember] public string prefab = "";
        [DataMember] public string display_name = "";
        [DataMember] public string localization_key = "";
        [DataMember] public int max_stack_size;
        [DataMember] public int max_quality;
        [DataMember] public string item_type = "";
        [DataMember] public bool localization_resolved;
    }
}
