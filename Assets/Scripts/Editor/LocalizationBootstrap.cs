using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// One-click setup of Unity Localization for the game: creates the ru/en/es/hi locales,
    /// the active LocalizationSettings, and the "GameText" string table populated with all
    /// translation keys. Re-runnable — updates existing entries in place.
    /// Menu: Tools ▸ Forge Game ▸ Setup Localization.
    /// </summary>
    public static class LocalizationBootstrap
    {
        private const string Dir = "Assets/Localization";
        private const string TablesDir = Dir + "/Tables";
        private const string Table = "GameText";

        // Column order after the key: ru, en, es, hi.
        private static readonly (string code, string name)[] Locales =
        {
            ("ru", "Русский"), ("en", "English"), ("es", "Español"), ("hi", "हिन्दी")
        };

        [MenuItem("Tools/Forge Game/Setup Localization")]
        public static void Setup()
        {
            EnsureFolder(Dir);
            EnsureFolder(TablesDir);

            // 1) Locales.
            var localeByCode = new System.Collections.Generic.Dictionary<string, Locale>();
            foreach (var (code, name) in Locales)
            {
                Locale loc = null;
                foreach (var l in LocalizationEditorSettings.GetLocales())
                    if (l.Identifier.Code == code) loc = l;
                if (loc == null)
                {
                    loc = Locale.CreateLocale(new LocaleIdentifier(code));
                    loc.name = "Locale_" + code;
                    AssetDatabase.CreateAsset(loc, $"{Dir}/Locale_{code}.asset");
                    LocalizationEditorSettings.AddLocale(loc);
                }
                localeByCode[code] = loc;
            }

            // 2) Active settings (default selectors include PlayerPref persistence).
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, $"{Dir}/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            // 3) String table collection.
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table);
            if (collection == null)
                collection = LocalizationEditorSettings.CreateStringTableCollection(Table, TablesDir);

            // 4) Entries.
            foreach (var row in Rows)
            {
                string key = row[0];
                var shared = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);
                for (int i = 0; i < Locales.Length; i++)
                {
                    string code = Locales[i].code, val = row[i + 1];
                    var table = collection.GetTable(localeByCode[code].Identifier) as StringTable
                                ?? collection.AddNewTable(localeByCode[code].Identifier) as StringTable;
                    var entry = table.GetEntry(shared.Id);
                    if (entry == null) table.AddEntry(shared.Id, val);
                    else entry.Value = val;
                    EditorUtility.SetDirty(table);
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Localization] Setup done: {Rows.Length} keys × {Locales.Length} locales.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // key, ru, en, es, hi
        private static readonly string[][] Rows =
        {
            new[]{"common.back","Назад","Back","Atrás","वापस"},
            new[]{"common.close","Закрыть","Close","Cerrar","बंद करें"},
            new[]{"common.yes","Да","Yes","Sí","हाँ"},
            new[]{"common.no","Нет","No","No","नहीं"},
            new[]{"common.confirm","Подтвердить","Confirm","Confirmar","पुष्टि करें"},
            new[]{"common.cancel","Отмена","Cancel","Cancelar","रद्द करें"},
            new[]{"common.empty","— пусто —","— empty —","— vacío —","— खाली —"},

            new[]{"common.loading","Загрузка…","Loading…","Cargando…","लोड हो रहा है…"},
            new[]{"common.apply","Применить","Apply","Aplicar","लागू करें"},
            new[]{"common.reset","Сбросить","Reset","Restablecer","रीसेट"},

            new[]{"menu.play","Играть","Play","Jugar","खेलें"},
            new[]{"menu.new_game","Новая игра","New Game","Nueva partida","नया खेल"},
            new[]{"menu.continue","Продолжить","Continue","Continuar","जारी रखें"},
            new[]{"menu.settings","Настройки","Settings","Ajustes","सेटिंग्स"},
            new[]{"menu.credits","Авторы","Credits","Créditos","श्रेय"},
            new[]{"menu.quit","Выход","Quit","Salir","बाहर निकलें"},
            new[]{"menu.tagline","Куй. Спускайся. Открывай.","Craft. Descend. Discover.","Forja. Desciende. Descubre.","ढालो. उतरो. खोजो।"},

            new[]{"settings.title","Настройки","Settings","Ajustes","सेटिंग्स"},
            new[]{"settings.language","Язык","Language","Idioma","भाषा"},
            new[]{"settings.master","Общая громкость","Master volume","Volumen general","मुख्य ध्वनि"},
            new[]{"settings.music","Музыка","Music","Música","संगीत"},
            new[]{"settings.sfx","Звуки","Sound effects","Efectos de sonido","ध्वनि प्रभाव"},
            new[]{"settings.fullscreen","Полный экран","Fullscreen","Pantalla completa","पूर्ण स्क्रीन"},
            new[]{"settings.tab_sound","Звук","Sound","Sonido","ध्वनि"},
            new[]{"settings.tab_graphics","Графика","Graphics","Gráficos","ग्राफिक्स"},
            new[]{"settings.tab_interface","Интерфейс","Interface","Interfaz","इंटरफ़ेस"},
            new[]{"settings.effects_slider","Эффекты","Effects","Efectos","प्रभाव"},
            new[]{"settings.mute","Отключить весь звук","Mute all","Silenciar todo","सब मौन"},
            new[]{"settings.window_mode","Режим экрана","Window mode","Modo de ventana","विंडो मोड"},
            new[]{"settings.resolution","Разрешение","Resolution","Resolución","रिज़ॉल्यूशन"},
            new[]{"settings.vsync","Вертикальная синхронизация","V-Sync","Sincronización vertical","वी-सिंक"},
            new[]{"settings.fps","Ограничение FPS","FPS limit","Límite de FPS","एफपीएस सीमा"},
            new[]{"settings.ui_scale","Масштаб интерфейса","UI scale","Escala de interfaz","यूआई स्केल"},
            new[]{"settings.screen_shake","Дрожание экрана","Screen shake","Vibración de pantalla","स्क्रीन कंपन"},
            new[]{"settings.effects_intensity","Интенсивность эффектов","Effects intensity","Intensidad de efectos","प्रभाव तीव्रता"},

            new[]{"hud.gold","Золото","Gold","Oro","सोना"},
            new[]{"hud.inventory","Инвентарь","Inventory","Inventario","सूची"},
            new[]{"hud.journal","Журнал","Journal","Diario","पत्रिका"},

            new[]{"inventory.title","Инвентарь","Inventory","Inventario","सूची"},
            new[]{"inventory.materials","Руда и материалы","Ores & materials","Minerales y materiales","अयस्क और सामग्री"},
            new[]{"inventory.ingots","Слитки","Ingots","Lingotes","सिल्लियाँ"},
            new[]{"inventory.blanks","Заготовки","Blanks","Piezas fundidas","ढलाई"},
            new[]{"inventory.weapons","Оружие","Weapons","Armas","हथियार"},

            new[]{"foundry.title","Плавильня","Foundry","Fundición","भट्ठी"},
            new[]{"foundry.choose_mold","Выберите форму для отливки","Choose a mold to cast","Elige un molde para fundir","ढालने के लिए साँचा चुनें"},
            new[]{"foundry.temperature","Температура","Temperature","Temperatura","तापमान"},
            new[]{"foundry.throw_log","Тащите бревно в огонь","Drag a log into the fire","Arrastra un tronco al fuego","लकड़ी को आग में खींचें"},
            new[]{"foundry.ready","Расплав готов","Melt ready","Fundido listo","पिघला तैयार"},
            new[]{"foundry.too_cold","Слишком холодно","Too cold","Demasiado frío","बहुत ठंडा"},
            new[]{"foundry.ores","Руды","Ores","Minerales","अयस्क"},
            new[]{"foundry.close_mold","Закрыть форму","Close mold","Cerrar molde","साँचा बंद करें"},
            new[]{"foundry.take_blank","Забрать заготовку","Take blank","Tomar pieza","ढलाई लें"},
            new[]{"foundry.melting","Руда плавится","Ore melting","Fundiendo mineral","अयस्क पिघल रहा है"},
            new[]{"foundry.overheat","Перегрев — металл портится","Overheating — metal spoiling","Sobrecalentándose — el metal se estropea","अधिक गरम — धातु ख़राब"},
            new[]{"foundry.hint_cold","Огонь остыл — подкиньте бревно, иначе руда не плавится.","The fire cooled — add a log or the ore won't melt.","El fuego se enfrió — añade un tronco o el mineral no se fundirá.","आग ठंडी हुई — लकड़ी डालें वरना अयस्क नहीं पिघलेगा।"},
            new[]{"foundry.hint_melting","Руда плавится — держите жар. Рано лить = слабая ковка.","Ore melting — keep it hot. Pour early = weak forging.","Fundiendo — mantén el calor. Verter pronto = forja débil.","अयस्क पिघल रहा है — गरम रखें। जल्दी डालना = कमज़ोर ढलाई।"},
            new[]{"foundry.hint_ready","Расплав готов — наклоните котёл, чтобы залить форму.","Melt ready — tilt the crucible to fill the mold.","Fundido listo — inclina el crisol para llenar el molde.","पिघला तैयार — साँचा भरने के लिए कड़ाही झुकाएँ।"},
            new[]{"foundry.hint_overheat","Металл перегревается и портится — лейте скорее!","Metal is overheating and spoiling — pour now!","El metal se sobrecalienta y se estropea — ¡vierte ya!","धातु अधिक गरम होकर ख़राब हो रही है — अभी डालें!"},

            new[]{"inventory.ingot_detail","масса {0} · чистота {1}% · качество {2}%","mass {0} · purity {1}% · quality {2}%","masa {0} · pureza {1}% · calidad {2}%","द्रव्यमान {0} · शुद्धता {1}% · गुणवत्ता {2}%"},
            new[]{"inventory.blank_detail","плавка {0}% · заливка {1}%","melt {0}% · pour {1}%","fundido {0}% · vertido {1}%","पिघलन {0}% · ढलाई {1}%"},
            new[]{"inventory.blank_name","Заготовка клинка ({0})","Blade blank ({0})","Pieza de hoja ({0})","ब्लेड ढलाई ({0})"},
            new[]{"inventory.blank_cast","Литая заготовка","Cast blank","Pieza fundida","ढली हुई ढलाई"},
            new[]{"inventory.weapon_detail","урон {0} · проч. {1} · {2} зол.","dmg {0} · dur {1} · {2} gold","daño {0} · dur {1} · {2} oro","क्षति {0} · टिकाऊपन {1} · {2} स्वर्ण"},
            new[]{"inventory.defects"," · дефектов: {0}"," · defects: {0}"," · defectos: {0}"," · दोष: {0}"},
            new[]{"inventory.flag_scrap","[лом]","[scrap]","[chatarra]","[स्क्रैप]"},
            new[]{"inventory.flag_porous","[пористый]","[porous]","[poroso]","[छिद्रयुक्त]"},
            new[]{"inventory.flag_overheated","[перегрет]","[overheated]","[sobrecalentado]","[अधिक गरम]"},
            new[]{"material.bronze","Бронза","Bronze","Bronce","कांस्य"},
            new[]{"material.bronze_desc","Тугоплавкая, надёжная","Refractory, reliable","Refractario, fiable","दुर्दम्य, विश्वसनीय"},

            new[]{"weapon.sword","Меч","Sword","Espada","तलवार"},
            new[]{"weapon.sickle","Серп","Sickle","Hoz","हँसिया"},
            new[]{"weapon.hammer","Молот","Hammer","Martillo","हथौड़ा"},
            new[]{"weapon.scythe","Коса","Scythe","Guadaña","दरांती"},
            new[]{"weapon.knife","Нож","Knife","Cuchillo","चाकू"},
            new[]{"weapon.pitchfork","Вилы","Pitchfork","Horca","पिचकाँटा"},
        };
    }
}
