using System.Collections.Generic;
using ForgeGame.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeGame.Dungeon
{
    /// <summary>
    /// Rank-based turn combat for the dungeon. The party (leader front, companion back)
    /// faces 1–3 enemies that emerge from the dark on the right. Turn order runs by speed;
    /// on a hero's turn the player picks one of three abilities (auto-targeted by the
    /// ability's reach rules), then enemies act. Hero HP persists across encounters — the
    /// "go deeper or die" tension — so a wipe returns the run to the smithy. Victory drops
    /// ore into the dungeon inventory and exploration resumes.
    ///
    /// The state machine is Update-timer driven (no coroutines) so it reads linearly and is
    /// easy to drive/verify. Only cosmetic lunges/flashes depend on frame time.
    /// </summary>
    public class DungeonCombat : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Transform party;          // fixed party root
        [SerializeField] private Transform leaderView;     // hero A (front)
        [SerializeField] private Transform companionView;  // hero B (back)
        [SerializeField] private float groundY = -3.2f;

        [Header("Art")]
        [SerializeField] private Sprite[] enemySprites;    // 0 Rat, 1 Goblin, 2 Brute
        [SerializeField] private Sprite hpBarSprite;       // white, LEFT pivot
        [SerializeField] private Sprite[] abilityIcons;    // 0 Strike,1 Break,2 Guard,3 Lunge,4 Heal

        [Header("Loot / links")]
        [SerializeField] private DungeonHotbar hotbar;
        [SerializeField] private Sprite oreSprite;
        [SerializeField] private string oreItemId = "iron_ore";
        [SerializeField] private DungeonSideController controller;

        [Header("HUD")]
        [SerializeField] private GameObject abilityBar;
        [SerializeField] private Button[] abilityButtons;      // 3
        [SerializeField] private Image[] abilityButtonIcons;   // 3
        [SerializeField] private TMP_Text[] abilityButtonLabels; // 3
        [SerializeField] private TMP_Text turnLabel;
        [SerializeField] private TMP_Text logLabel;
        [SerializeField] private GameObject bannerObject;
        [SerializeField] private TMP_Text bannerLabel;

        public bool InCombat { get; private set; }

        // ---- combat model ----

        private enum AbilityKind { StrikeFront, BreakFront, Guard, LungeAny, Heal }

        private class Ability
        {
            public string nameKey; public int icon; public AbilityKind kind;
            public int damage; public int heal; public int cooldown;
        }

        private class Combatant
        {
            public string nameKey;   // localization key; resolve with Loc.Tr for display
            public bool isEnemy;
            public int rank;
            public int maxHp, hp;
            public int attack, speed;
            public bool alive => hp > 0;

            public Transform view;
            public SpriteRenderer sr;
            public Color baseColor = Color.white;
            public float flash;          // red/green flash timer
            public Color flashColor = Color.white;
            public bool stunned;
            public bool guard;

            public Ability[] abilities;  // heroes only
            public int[] cd;             // per-ability cooldown, heroes only

            public Transform barBg, barFill;
        }

        private enum Phase { Idle, HeroTurn, Anim, EnemyTelegraph, Ended }

        private Phase _phase = Phase.Idle;
        private float _timer;
        private Combatant _leader, _companion;
        private readonly List<Combatant> _heroes = new List<Combatant>();
        private readonly List<Combatant> _enemies = new List<Combatant>();
        private readonly List<Combatant> _order = new List<Combatant>();
        private int _orderIdx;
        private Combatant _current, _pendingTarget;

        // Animation.
        private Combatant _animActor, _animTarget;
        private Vector3 _animBase, _animDir;
        private float _animLen;

        private Transform _barRoot;
        private bool _win;

        private const float EnemyTelegraph = 0.5f;
        private const float ActAnim = 0.4f;
        private const float BannerTime = 1.7f;
        private const float BarWidth = 1.0f;

        // =============================================================
        //  Public API
        // =============================================================

        public void StartEncounter(int depth)
        {
            if (InCombat) return;
            if (_barRoot == null) _barRoot = new GameObject("CombatBars").transform;
            EnsureHeroes();

            // Heroes reset their per-fight state but KEEP hp.
            foreach (var h in _heroes) { h.guard = false; h.stunned = false; for (int i = 0; i < h.cd.Length; i++) h.cd[i] = 0; RefreshBar(h); }

            SpawnEnemies(depth);
            InCombat = true;
            if (controller != null) controller.SetCombatMode(true);
            if (bannerObject != null) bannerObject.SetActive(false);
            if (logLabel != null) logLabel.text = Loc.Tr("combat.encounter");
            BeginRound();
        }

        // Wired to the three ability buttons.
        public void ClickAbility(int index)
        {
            if (_phase != Phase.HeroTurn || _current == null || _current.isEnemy) return;
            if (index < 0 || index >= _current.abilities.Length) return;
            var ab = _current.abilities[index];
            if (ab == null || _current.cd[index] > 0) return;

            Combatant target = SelectTarget(ab);
            PerformHeroAbility(_current, ab, index, target);
        }

        // =============================================================
        //  Setup
        // =============================================================

        private void EnsureHeroes()
        {
            if (_heroes.Count > 0) return;
            _leader = new Combatant
            {
                nameKey = "hero.knight", isEnemy = false, rank = 0, maxHp = 32, hp = 32, attack = 6, speed = 5,
                view = leaderView, sr = leaderView != null ? leaderView.GetComponent<SpriteRenderer>() : null,
                abilities = new[]
                {
                    new Ability { nameKey = "ability.strike", icon = 0, kind = AbilityKind.StrikeFront, damage = 6, cooldown = 0 },
                    new Ability { nameKey = "ability.break",  icon = 1, kind = AbilityKind.BreakFront,  damage = 9, cooldown = 2 },
                    new Ability { nameKey = "ability.guard",  icon = 2, kind = AbilityKind.Guard,        cooldown = 2 },
                },
                cd = new int[3],
            };
            _companion = new Combatant
            {
                nameKey = "hero.companion", isEnemy = false, rank = 1, maxHp = 22, hp = 22, attack = 4, speed = 6,
                view = companionView, sr = companionView != null ? companionView.GetComponent<SpriteRenderer>() : null,
                abilities = new[]
                {
                    new Ability { nameKey = "ability.lunge",   icon = 3, kind = AbilityKind.LungeAny, damage = 5, cooldown = 0 },
                    new Ability { nameKey = "ability.bandage", icon = 4, kind = AbilityKind.Heal,     heal = 10, cooldown = 2 },
                    new Ability { nameKey = "ability.strike",  icon = 0, kind = AbilityKind.StrikeFront, damage = 4, cooldown = 0 },
                },
                cd = new int[3],
            };
            // Apply the smithy loadout: each hero comes only if chosen. Equipped weapons add
            // flat damage to that hero's strikes. (Un-configured = full default party.)
            bool configured = ExpeditionLoadout.configured;
            bool takeLeader = !configured || ExpeditionLoadout.takeLeader;
            bool takeCompanion = !configured || ExpeditionLoadout.takeCompanion;

            if (takeLeader)
            {
                ApplyWeaponBonus(_leader, configured ? ExpeditionLoadout.leaderWeaponBonus : 0);
                if (_leader.sr != null) _leader.baseColor = _leader.sr.color;
                _heroes.Add(_leader); CreateBar(_leader);
            }
            else
            {
                _leader = null;
                if (leaderView != null) leaderView.gameObject.SetActive(false);
            }

            if (takeCompanion)
            {
                ApplyWeaponBonus(_companion, configured ? ExpeditionLoadout.companionWeaponBonus : 0);
                if (_companion.sr != null) _companion.baseColor = _companion.sr.color;
                _heroes.Add(_companion); CreateBar(_companion);
            }
            else
            {
                _companion = null;
                if (companionView != null) companionView.gameObject.SetActive(false);
            }
        }

        private void ApplyWeaponBonus(Combatant hero, int bonus)
        {
            if (bonus <= 0 || hero.abilities == null) return;
            foreach (var a in hero.abilities) if (a.damage > 0) a.damage += bonus;
        }

        private void SpawnEnemies(int depth)
        {
            _enemies.Clear();
            int count = depth < 12 ? Random.Range(1, 3) : Random.Range(2, 4);
            float scale = 1f + depth * 0.015f;
            float baseX = party != null ? party.position.x + 3.4f : 3.4f;

            for (int i = 0; i < count; i++)
            {
                int type = PickType(depth);
                var (hp, atk, spd, worldH) = type switch
                {
                    2 => (Mathf.RoundToInt(26 * scale), Mathf.RoundToInt(7 * scale), 3, 2.1f),   // Brute
                    1 => (Mathf.RoundToInt(14 * scale), Mathf.RoundToInt(4 * scale), 5, 1.7f),   // Goblin
                    _ => (Mathf.RoundToInt(8 * scale),  Mathf.RoundToInt(2 * scale), 7, 1.1f),   // Rat
                };
                string nmKey = type == 2 ? "enemy.brute" : type == 1 ? "enemy.goblin" : "enemy.rat";

                var go = new GameObject("Enemy_" + nmKey);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = enemySprites != null && type < enemySprites.Length ? enemySprites[type] : null;
                sr.sortingOrder = 8;
                float sc = sr.sprite != null ? worldH / (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) : 1f;
                go.transform.localScale = new Vector3(sc, sc, 1f);
                go.transform.position = new Vector3(baseX + i * 1.45f, groundY + worldH * 0.5f, 0f);

                var e = new Combatant
                {
                    nameKey = nmKey, isEnemy = true, rank = i, maxHp = hp, hp = hp, attack = atk, speed = spd,
                    view = go.transform, sr = sr, baseColor = sr.color,
                };
                _enemies.Add(e);
                CreateBar(e);
            }
        }

        private int PickType(int depth)
        {
            float r = Random.value;
            if (depth < 10) return r < 0.7f ? 0 : 1;                 // rats, some goblins
            if (depth < 22) return r < 0.35f ? 0 : r < 0.8f ? 1 : 2; // mixed
            return r < 0.2f ? 1 : 2;                                 // goblins + brutes
        }

        // =============================================================
        //  Round / turn flow
        // =============================================================

        private void BeginRound()
        {
            _order.Clear();
            foreach (var h in _heroes) if (h.alive) _order.Add(h);
            foreach (var e in _enemies) if (e.alive) _order.Add(e);
            _order.Sort((a, b) => b.speed != a.speed ? b.speed.CompareTo(a.speed) : (a.isEnemy ? 1 : -1));
            _orderIdx = 0;
            NextActor();
        }

        private void NextActor()
        {
            if (_phase == Phase.Ended) return;
            while (true)
            {
                if (_orderIdx >= _order.Count) { BeginRound(); return; }
                var actor = _order[_orderIdx++];
                if (!actor.alive) continue;
                _current = actor;
                if (actor.isEnemy) StartEnemyTurn(actor);
                else StartHeroTurn(actor);
                return;
            }
        }

        private void StartHeroTurn(Combatant hero)
        {
            for (int i = 0; i < hero.cd.Length; i++) if (hero.cd[i] > 0) hero.cd[i]--;
            hero.guard = false; // brace lasted through the enemies' round; it lapses on his turn
            _phase = Phase.HeroTurn;
            if (turnLabel != null) turnLabel.text = Loc.Format("combat.turn", Loc.Tr(hero.nameKey));
            ShowAbilities(hero);
        }

        private void StartEnemyTurn(Combatant enemy)
        {
            HideAbilities();
            if (enemy.stunned)
            {
                enemy.stunned = false;
                Log(Loc.Format("combat.stunned", Loc.Tr(enemy.nameKey)));
                Flash(enemy, new Color(0.8f, 0.8f, 1f));
                _phase = Phase.Anim; _timer = 0.35f; _animActor = null; _animTarget = null;
                return;
            }
            _pendingTarget = FrontHero();
            if (_pendingTarget == null) { CheckEnd(); return; }
            if (turnLabel != null) turnLabel.text = Loc.Format("combat.turn", Loc.Tr(enemy.nameKey));
            Flash(enemy, new Color(1f, 0.85f, 0.5f)); // telegraph
            _phase = Phase.EnemyTelegraph; _timer = EnemyTelegraph;
        }

        private void ResolveEnemyAttack(Combatant enemy)
        {
            var t = _pendingTarget != null && _pendingTarget.alive ? _pendingTarget : FrontHero();
            if (t == null) { CheckEnd(); return; }
            int dmg = enemy.attack + Random.Range(0, 2);
            if (t.guard) dmg = Mathf.Max(1, Mathf.CeilToInt(dmg * 0.5f));
            t.hp = Mathf.Max(0, t.hp - dmg);
            Flash(t, new Color(1f, 0.3f, 0.25f));
            RefreshBar(t);
            Log(Loc.Format("combat.log_hit", Loc.Tr(enemy.nameKey), Loc.Tr(t.nameKey), dmg) + (t.guard ? Loc.Tr("combat.guarded") : ""));
            BeginAnim(enemy, t);
            if (!CheckEnd()) { _phase = Phase.Anim; _timer = ActAnim; }
        }

        private void PerformHeroAbility(Combatant hero, Ability ab, int index, Combatant target)
        {
            switch (ab.kind)
            {
                case AbilityKind.Guard:
                    hero.guard = true; Flash(hero, new Color(0.6f, 0.8f, 1f));
                    Log(Loc.Format("combat.log_guard", Loc.Tr(hero.nameKey)));
                    break;
                case AbilityKind.Heal:
                    if (target != null)
                    {
                        int before = target.hp;
                        target.hp = Mathf.Min(target.maxHp, target.hp + ab.heal);
                        Flash(target, new Color(0.4f, 1f, 0.5f)); RefreshBar(target);
                        Log(Loc.Format("combat.log_heal", Loc.Tr(hero.nameKey), Loc.Tr(target.nameKey), target.hp - before));
                    }
                    break;
                default: // damage abilities
                    if (target != null)
                    {
                        int dmg = ab.damage + Random.Range(0, 2);
                        target.hp = Mathf.Max(0, target.hp - dmg);
                        Flash(target, new Color(1f, 0.4f, 0.3f)); RefreshBar(target);
                        bool stun = ab.kind == AbilityKind.BreakFront;
                        if (stun) target.stunned = true;
                        Log(Loc.Format("combat.log_ability", Loc.Tr(hero.nameKey), Loc.Tr(ab.nameKey), Loc.Tr(target.nameKey), dmg)
                            + (stun ? Loc.Tr("combat.stun_suffix") : ""));
                        BeginAnim(hero, target);
                    }
                    break;
            }
            hero.cd[index] = ab.cooldown;
            HideAbilities();
            if (!CheckEnd()) { _phase = Phase.Anim; _timer = ActAnim; }
        }

        // Auto-targeting by ability reach.
        private Combatant SelectTarget(Ability ab)
        {
            switch (ab.kind)
            {
                case AbilityKind.Guard: return _current;
                case AbilityKind.Heal: return MostInjuredHero();
                case AbilityKind.LungeAny: return LowestHpEnemy();
                default: return FrontEnemy();
            }
        }

        private Combatant FrontEnemy()
        {
            Combatant best = null;
            foreach (var e in _enemies) if (e.alive && (best == null || e.rank < best.rank)) best = e;
            return best;
        }

        private Combatant LowestHpEnemy()
        {
            Combatant best = null;
            foreach (var e in _enemies) if (e.alive && (best == null || e.hp < best.hp)) best = e;
            return best;
        }

        private Combatant FrontHero()
        {
            if (_leader != null && _leader.alive) return _leader;
            if (_companion != null && _companion.alive) return _companion;
            return null;
        }

        private Combatant MostInjuredHero()
        {
            Combatant best = null; float worst = 2f;
            foreach (var h in _heroes)
            {
                if (!h.alive) continue;
                float f = h.hp / (float)h.maxHp;
                if (f < worst) { worst = f; best = h; }
            }
            return best;
        }

        private bool CheckEnd()
        {
            bool enemiesLeft = false; foreach (var e in _enemies) if (e.alive) { enemiesLeft = true; break; }
            bool heroesLeft = false; foreach (var h in _heroes) if (h.alive) { heroesLeft = true; break; }
            if (!enemiesLeft) { Victory(); return true; }
            if (!heroesLeft) { Defeat(); return true; }
            return false;
        }

        private void Victory()
        {
            _phase = Phase.Ended; _win = true; _timer = BannerTime;
            HideAbilities();
            int loot = Random.Range(2, 5);
            if (hotbar != null) hotbar.Add(oreItemId, oreSprite, loot);
            ExpeditionResult.Add(oreItemId, loot); // banked into the smithy on return
            ShowBanner(Loc.Format("combat.victory", loot));
            if (turnLabel != null) turnLabel.text = "";
        }

        private void Defeat()
        {
            _phase = Phase.Ended; _win = false; _timer = BannerTime;
            HideAbilities();
            ShowBanner(Loc.Tr("combat.defeat"));
            if (turnLabel != null) turnLabel.text = "";
        }

        private void FinishEncounter()
        {
            foreach (var e in _enemies) { if (e.barBg) Destroy(e.barBg.gameObject); if (e.barFill) Destroy(e.barFill.gameObject); if (e.view) Destroy(e.view.gameObject); }
            _enemies.Clear();
            InCombat = false;
            _phase = Phase.Idle;
            if (bannerObject != null) bannerObject.SetActive(false);
            if (abilityBar != null) abilityBar.SetActive(false);
            if (controller != null) controller.SetCombatMode(false);
            if (controller != null) controller.OnCombatEnd(_win);
        }

        // =============================================================
        //  Update: timers + cosmetics
        // =============================================================

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateFlash(dt);
            HideDeadEnemies();
            UpdateBars();

            switch (_phase)
            {
                case Phase.EnemyTelegraph:
                    _timer -= dt;
                    if (_timer <= 0f) ResolveEnemyAttack(_current);
                    break;
                case Phase.Anim:
                    _timer -= dt;
                    UpdateLunge(1f - Mathf.Clamp01(_timer / ActAnim));
                    if (_timer <= 0f) { EndLunge(); NextActor(); }
                    break;
                case Phase.Ended:
                    _timer -= dt;
                    if (_timer <= 0f) FinishEncounter();
                    break;
            }
        }

        private void BeginAnim(Combatant actor, Combatant target)
        {
            _animActor = actor; _animTarget = target;
            if (actor != null && target != null)
            {
                _animBase = actor.view.position;
                _animDir = (target.view.position - actor.view.position).normalized;
                _animLen = 0.5f;
            }
        }

        private void UpdateLunge(float p)
        {
            if (_animActor == null || _animActor.view == null) return;
            float off = Mathf.Sin(p * Mathf.PI) * _animLen;
            _animActor.view.position = _animBase + _animDir * off;
        }

        private void EndLunge()
        {
            if (_animActor != null && _animActor.view != null) _animActor.view.position = _animBase;
            _animActor = null; _animTarget = null;
        }

        // A slain enemy vanishes once its death flash has played out (its HP bar is already
        // hidden by UpdateBars). The GameObject is finally destroyed when the fight ends.
        private void HideDeadEnemies()
        {
            foreach (var e in _enemies)
                if (!e.alive && e.view != null && e.view.gameObject.activeSelf && e.flash <= 0f)
                    e.view.gameObject.SetActive(false);
        }

        private void Flash(Combatant c, Color col) { if (c != null) { c.flash = 0.28f; c.flashColor = col; } }

        private void UpdateFlash(float dt)
        {
            foreach (var c in AllCombatants())
            {
                if (c.sr == null) continue;
                if (c.flash > 0f)
                {
                    c.flash -= dt;
                    c.sr.color = Color.Lerp(c.baseColor, c.flashColor, Mathf.Clamp01(c.flash / 0.28f));
                }
                else c.sr.color = c.baseColor;
            }
        }

        private IEnumerable<Combatant> AllCombatants()
        {
            foreach (var h in _heroes) yield return h;
            foreach (var e in _enemies) yield return e;
        }

        // =============================================================
        //  HP bars (world space, left-pivot fill)
        // =============================================================

        private void CreateBar(Combatant c)
        {
            if (hpBarSprite == null || _barRoot == null) return;
            var bg = new GameObject("BarBg_" + c.nameKey).transform; bg.SetParent(_barRoot, false);
            var bgSr = bg.gameObject.AddComponent<SpriteRenderer>();
            bgSr.sprite = hpBarSprite; bgSr.color = new Color(0.05f, 0.04f, 0.04f, 0.9f); bgSr.sortingOrder = 20;
            bg.localScale = new Vector3(BarWidth, 0.14f, 1f);

            var fill = new GameObject("BarFill_" + c.nameKey).transform; fill.SetParent(_barRoot, false);
            var fSr = fill.gameObject.AddComponent<SpriteRenderer>();
            fSr.sprite = hpBarSprite; fSr.color = new Color(0.3f, 0.8f, 0.35f); fSr.sortingOrder = 21;
            fill.localScale = new Vector3(BarWidth, 0.14f, 1f);

            c.barBg = bg; c.barFill = fill;
            RefreshBar(c);
        }

        private void RefreshBar(Combatant c)
        {
            if (c.barFill == null) return;
            float f = c.maxHp > 0 ? Mathf.Clamp01(c.hp / (float)c.maxHp) : 0f;
            c.barFill.localScale = new Vector3(BarWidth * f, 0.14f, 1f);
            var sr = c.barFill.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = f > 0.5f ? new Color(0.3f, 0.8f, 0.35f) : f > 0.25f ? new Color(0.9f, 0.75f, 0.25f) : new Color(0.85f, 0.28f, 0.2f);
        }

        private void UpdateBars()
        {
            foreach (var c in AllCombatants())
            {
                if (c.barBg == null || c.view == null) continue;
                bool show = c.alive && InCombat;
                c.barBg.gameObject.SetActive(show);
                c.barFill.gameObject.SetActive(show);
                if (!show) continue;
                float topY = c.view.position.y + BarTop(c);
                var p = new Vector3(c.view.position.x - BarWidth * 0.5f, topY, 0f);
                c.barBg.position = p; c.barFill.position = p;
            }
        }

        private float BarTop(Combatant c)
        {
            if (c.sr != null && c.sr.sprite != null)
                return c.sr.sprite.rect.height / c.sr.sprite.pixelsPerUnit * c.view.lossyScale.y * 0.5f + 0.25f;
            return 1.6f;
        }

        // =============================================================
        //  HUD helpers
        // =============================================================

        private void ShowAbilities(Combatant hero)
        {
            if (abilityBar != null) abilityBar.SetActive(true);
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                bool has = hero.abilities != null && i < hero.abilities.Length;
                if (abilityButtons[i] != null) abilityButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var ab = hero.abilities[i];
                if (abilityButtonIcons != null && i < abilityButtonIcons.Length && abilityButtonIcons[i] != null)
                    abilityButtonIcons[i].sprite = abilityIcons != null && ab.icon < abilityIcons.Length ? abilityIcons[ab.icon] : null;
                bool ready = hero.cd[i] <= 0;
                if (abilityButtons[i] != null) abilityButtons[i].interactable = ready;
                if (abilityButtonLabels != null && i < abilityButtonLabels.Length && abilityButtonLabels[i] != null)
                {
                    string nm = Loc.Tr(ab.nameKey);
                    abilityButtonLabels[i].text = ready ? nm : $"{nm} ({hero.cd[i]})";
                }
            }
        }

        private void HideAbilities()
        {
            if (abilityBar != null) abilityBar.SetActive(false);
        }

        private void ShowBanner(string text)
        {
            if (bannerObject != null) bannerObject.SetActive(true);
            if (bannerLabel != null) bannerLabel.text = text;
        }

        private void Log(string text) { if (logLabel != null) logLabel.text = text; }
    }
}
