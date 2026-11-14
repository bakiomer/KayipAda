// SoundLibrary.cs
// Hangi ses türünde (SoundId) hangi ses dosyalarının çalınacağını tutan ayar dosyası.
// Bir türde birden fazla ses varsa her seferinde rastgele biri çalınır (tekrar hissi azalır).
// Dosya: Assets/_Game/Data/SoundLibrary.asset. Ses eksikse hiçbir şey çalmaz, hata vermez.

using System;
using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Kayıp Ada/Ses Kütüphanesi")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SoundId Id;
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume = 1f;
        }

        [SerializeField] private Entry[] entries = new Entry[0];
        [Tooltip("Arka plandaki dalga/doğa sesi. Boşsa oyun kendi yumuşak dalga sesini üretir.")]
        [SerializeField] private AudioClip ambient;

        public AudioClip Ambient => ambient;

        public Entry Find(SoundId id)
        {
            foreach (Entry e in entries)
            {
                if (e != null && e.Id == id) return e;
            }
            return null;
        }

#if UNITY_EDITOR
        // Var olan seçimlere dokunmadan, listede olmayan ses türlerini ekler.
        public void EditorAddMissing(Entry[] defaults)
        {
            System.Collections.Generic.List<Entry> list = new System.Collections.Generic.List<Entry>(entries);
            foreach (Entry d in defaults)
            {
                if (Find(d.Id) == null) list.Add(d);
            }
            entries = list.ToArray();
        }

        public void EditorSetup(Entry[] newEntries, AudioClip newAmbient)
        {
            entries = newEntries;
            ambient = newAmbient;
        }
#endif
    }
}
