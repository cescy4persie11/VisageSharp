using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Ensage;
using Ensage.Items;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.Common.Objects;
using SharpDX.Direct3D9;
using SharpDX;
using System.Diagnostics;
using System.Windows.Input;
using Ensage.Common.Extensions.SharpDX;

namespace VisageSharp
{
    class Program
    {
        private static bool _loaded;
        private static Hero _me;
        //public static IEnumerable<TrackingProjectile> myProjectiles;
        //public static bool myAttackInAir;
        public static Ability _Q;
        public static Ability _W;
        public static Ability _E;
        public static Ability _R;
        //private static int familiarAttackDmg;
        private static Hero killTarget;
        private static ParticleEffect meToTargetParticleEffect;
        private static int autoAttackMode = 2;
        private static bool familiarAttacked;
        private static bool FollowHasLock;
        private static bool LasthitHasLock;
        private static bool FamiliarBeingAttackedDrawingEn;
        
        //private static bool hasLens;

        private static readonly string Ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly Menu Menu = new Menu("VisageSharp", "VisageSharp", true, "npc_dota_hero_visage", true);
        private static readonly MenuItem AutoLastHit = new MenuItem("Auto Familar Lasthit", "Auto Familar Lasthit");
        private static readonly MenuItem AutoSoulAssump = new MenuItem("AutoSoulAssump", "AutoSoulAssump");
        private static readonly MenuItem SoloKill = new MenuItem("SoloKill", "SoloKill");
        private static readonly MenuItem FamiliarFollow = new MenuItem("FamiliarFollow", "FamiliarFollow");

        static void Main(string[] args)
        {
            Menu.AddItem(AutoLastHit.SetValue(new KeyBind('W', KeyBindType.Toggle)));
            Menu.AddItem(AutoSoulAssump.SetValue(new KeyBind('X', KeyBindType.Toggle, true)).SetTooltip("always spit max-nuke, recommend always on"));
            Menu.AddItem(SoloKill.SetValue(new KeyBind('D', KeyBindType.Toggle)).SetTooltip("Enabled in team fight, damage mode"));
            Menu.AddItem(new MenuItem("LockTarget", "Lock Target in Combo").SetValue(true).SetTooltip("This will lock the target while in combo"));
            Menu.AddItem(FamiliarFollow.SetValue(new KeyBind('E', KeyBindType.Toggle, true)).SetTooltip("let familiars follow you in position, but never auto-attack"));
            Menu.AddToMainMenu();
            Drawing.OnDraw += Drawing_OnDraw_familiarLastHit;
            Drawing.OnDraw += Drawing_OnDraw_AutoSoulAssump;
            Drawing.OnDraw += Drawing_OnDraw_SoloKill;
            Drawing.OnDraw += Drawing_OnDraw_Follow;
            Game.OnUpdate += Game_OnUpdate_Infos;
            Player.OnExecuteOrder += Player_OnExecuteAction;
            Game.OnUpdate += Game_OnUpdate_AutoFamaliarLastHit;
            Game.OnUpdate += Game_OnUpdate_NukeOn;
            Game.OnUpdate += Game_OnUpdate_SoloKill;
            Game.OnUpdate += Game_OnUpdate_FamiliarControl;
            Game.OnUpdate += Game_OnUpdate_Follow;
        }

        private static void Player_OnExecuteAction(Player sender, ExecuteOrderEventArgs args)
        {
            var _me = ObjectManager.LocalHero;
            if (autoAttackMode != 2)
            {
                autoAttackMode = 2;
                Game.ExecuteCommand("dota_player_units_auto_attack_mode " + autoAttackMode);
            } 


            if (!_me.IsAlive)
            {
                //SoloKill.SetValue(new KeyBind(SoloKill.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                AutoSoulAssump.SetValue(new KeyBind(AutoSoulAssump.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
            }

            var familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);
            var EnemyNearby = ObjectManager.GetEntities<Hero>().Where(x => x.IsAlive
                                                                          && x.Team != _me.Team
                                                                          && x.Distance2D(_me) <= 600);
            var AnyfamiliarNearby = ObjectManager.GetEntities<Unit>().Any(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar
                                                                          && x.IsAlive && x.IsAlive && x.Team == _me.Team
                                                                          && x.Distance2D(_me) <= 400);
            switch (args.Order)
            {

                case Order.AttackTarget:
                    {
                        
                        break;
                    }
                case Order.AttackLocation:
                    {

                        break;
                    }
                case Order.AbilityTarget:
                    {
                         break;
                    }
                case Order.AbilityLocation:
                    {
                        break;
                    }
                case Order.Ability:
                    {                       
                        break;
                    }
                case Order.MoveLocation:
                    {
                        if (AnyfamiliarNearby)
                        {
                            if (EnemyNearby.Count() == 0)
                            {
                                if (Menu.Item("FamiliarFollow").GetValue<KeyBind>().Active && familiars != null)
                                {
                                    foreach (var f in familiars)
                                    {
                                        if (f.CanMove())
                                        {
                                            f.Move(Game.MousePosition);
                                        }
                                    }
                                }
                            }
                            else
                            {
                            }
                        }            
                        break;
                    }
                case Order.MoveTarget:
                    {
                        break;
                    }
                case Order.Stop:
                    {
                        //disable the combo
                        SoloKill.SetValue(new KeyBind(SoloKill.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        //AutoSoulAssump.SetValue(new KeyBind(AutoSoulAssump.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        break;
                    }
                case Order.Hold:
                    {
                        //disabled combo
                        SoloKill.SetValue(new KeyBind(SoloKill.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        //AutoSoulAssump.SetValue(new KeyBind(AutoSoulAssump.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        break;
                    }
                case Order.ToggleAbility:
                    {
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private static void Drawing_OnDraw_Follow(EventArgs args)
        {
            if (!_loaded) return;
            var familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);

            //if (familiars == null) return;
            if (AutoLastHit.GetValue<KeyBind>().Active) return;
            if (SoloKill.GetValue<KeyBind>().Active) return;
            if (!FamiliarFollow.GetValue<KeyBind>().Active) return;

            Drawing.DrawText("FollowMode(" + Utils.KeyToText(FamiliarFollow.GetValue<KeyBind>().Key) + ")", new Vector2(Drawing.Width - 100, 100) + new Vector2(-15, 15), new Vector2(20), new Color(255, 255, 0),
                    FontFlags.AntiAlias | FontFlags.DropShadow | FontFlags.Additive | FontFlags.Custom |
                    FontFlags.StrikeOut);
        }

        private static void Drawing_OnDraw_familiarLastHit(EventArgs args)
        {
            if (!_loaded) return;
            var Familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);
            if (FamiliarBeingAttackedDrawingEn)
            {
                Drawing.DrawText("Familiars", new Vector2(Drawing.Width - 100, 130) + new Vector2(-15, 15), new Vector2(20), new Color(255, 0, 0),
                    FontFlags.AntiAlias | FontFlags.DropShadow | FontFlags.Additive | FontFlags.Custom |
                    FontFlags.StrikeOut);
                Drawing.DrawText("UnderAttack(" + Utils.KeyToText(AutoLastHit.GetValue<KeyBind>().Key) + ")", new Vector2(Drawing.Width - 120, 150) + new Vector2(-15, 15), new Vector2(20), new Color(255, 0, 0),
                    FontFlags.AntiAlias | FontFlags.DropShadow | FontFlags.Additive | FontFlags.Custom |
                    FontFlags.StrikeOut);
            }else if (Menu.Item("Auto Familar Lasthit").GetValue<KeyBind>().Active)
            {
                var startPos = new Vector2(Drawing.Width - 75, 150);
                var size = new Vector2(150, 40);
                Drawing.DrawText("Last Hit(" + Utils.KeyToText(AutoLastHit.GetValue<KeyBind>().Key) + ")", startPos + new Vector2(-15, 15), new Vector2(20), new Color(0, 155, 255),
                        FontFlags.AntiAlias | FontFlags.DropShadow | FontFlags.Additive | FontFlags.Custom |
                        FontFlags.StrikeOut);
                
                if(Familiars != null)
                {
                    var name = "materials/ensage_ui/modifier_textures/visage_summon_familiars.vmat";
                    size = new Vector2(50, 50);
                    Drawing.DrawRect(startPos + new Vector2(-70,0), size,
                        Drawing.GetTexture(name));
                }
            }
        }

        private static void Drawing_OnDraw_AutoSoulAssump(EventArgs args)
        {
            if (!_loaded) return;
            var Familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.IsAlive && x.Team == _me.Team);

            if (Menu.Item("AutoSoulAssump").GetValue<KeyBind>().Active)
            {
                var startPos = new Vector2(Drawing.Width - 100, 250);
                var size = new Vector2(90, 90);
                //Drawing.DrawRect(startPos, size, new Color(0, 0, 0, 100));
                //Drawing.DrawRect(startPos, size, new Color(0, 0, 0, 255), true);
                Drawing.DrawText("Nuke On(" + Utils.KeyToText(AutoSoulAssump.GetValue<KeyBind>().Key) + ")", startPos + new Vector2(0, 10), new Vector2(20), new Color(0, 155, 255),
                    FontFlags.AntiAlias | FontFlags.DropShadow | FontFlags.Additive | FontFlags.Custom |
                    FontFlags.StrikeOut);
                if (Familiars != null)
                {
                    var name = "materials/ensage_ui/spellicons/visage_soul_assumption.vmat";
                    size = new Vector2(50, 50);
                    Drawing.DrawRect(startPos + new Vector2(10, 35), size + new Vector2(13, -6),
                                    Drawing.GetTexture(name));
                    Drawing.DrawRect(startPos + new Vector2(10, 35), size + new Vector2(14, -5),
                                    new Color(0, 0, 0, 255), true);
                }
            }
        }

        private static void Drawing_OnDraw_SoloKill(EventArgs args)
        {
            if (!_loaded) return;
            var Familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.IsAlive && x.Team == _me.Team);

            try
            {
                if (Menu.Item("LockTarget").GetValue<bool>())
                {

                    if (killTarget == null)
                    {
                        killTarget = _me.ClosestToMouseTarget(1000);
                    }
                }
                else
                {
                    killTarget = _me.ClosestToMouseTarget(1000);
                }

                if (killTarget == null)
                {

                }
                else
                {
                    
                }
                //select the target
                //there is no priority between initiate zip and chase zip
                if (!SoloKill.GetValue<KeyBind>().Active)
                {
                    killTarget = null;
                    meToTargetParticleEffect.Dispose();
                    meToTargetParticleEffect = null;
                    return;
                }
                else
                {
                    if (SoloKill.GetValue<KeyBind>().Active)
                    {
                        // disable last hit?
                        var startPos = new Vector2(Drawing.Width - 100, 350);
                        var size = new Vector2(90, 90);
                        Drawing.DrawRect(startPos, size, new Color(0, 0, 0, 100));
                        Drawing.DrawRect(startPos, size, new Color(0, 0, 0, 255), true);
                        Drawing.DrawText("Combo(" + Utils.KeyToText(SoloKill.GetValue<KeyBind>().Key) + ")", startPos + new Vector2(8, 10), new Vector2(20), new Color(0, 155, 255),
                            FontFlags.AntiAlias | FontFlags.DropShadow | FontFlags.Additive | FontFlags.Custom |
                            FontFlags.StrikeOut);
                        if (killTarget != null)
                        {
                            if (killTarget.IsAlive)
                            {
                                var name = "materials/ensage_ui/heroes_horizontal/" + killTarget.Name.Replace("npc_dota_hero_", "") + ".vmat";
                                size = new Vector2(50, 50);
                                Drawing.DrawRect(startPos + new Vector2(10, 35), size + new Vector2(13, -6),
                                    Drawing.GetTexture(name));
                                Drawing.DrawRect(startPos + new Vector2(10, 35), size + new Vector2(14, -5),
                                    new Color(0, 0, 0, 255), true);
                            }
                        }
                    }else
                    {
                        //disable both
                    }

                    if (_me.IsAlive && killTarget != null && killTarget.IsValid && !killTarget.IsIllusion && killTarget.IsAlive && killTarget.IsVisible)
                        DrawTarget(killTarget);
                    else if (meToTargetParticleEffect != null)
                    {
                        meToTargetParticleEffect.Dispose();
                        meToTargetParticleEffect = null;
                    }
                }
            }
            catch
            {

            }
        }

        private static void Game_OnUpdate_Follow(EventArgs args)
        {
            _me = ObjectManager.LocalHero;
            #region standard checks for loader and in-game status
            if (!_loaded)
            {
                if (!Game.IsInGame || _me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Visage)
                {
                    return;
                }
                _loaded = true;
                Console.Write("VisageSharp Loaded");
            }

            if (!Game.IsInGame || _me == null)
            {
                _loaded = false;
                return;
            }
            if (Game.IsPaused) return;
            #endregion
            //disable AutoLastHit
            var familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);

            var AnyfamiliarNearby = ObjectManager.GetEntities<Unit>().Any(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar
                                                                          && x.IsAlive && x.IsAlive && x.Team == _me.Team
                                                                          && x.Distance2D(_me) <= 400);
          

            
            if (!FamiliarFollow.GetValue<KeyBind>().Active) {
                FollowHasLock = true;
                return;
            }
            if (FamiliarFollow.GetValue<KeyBind>().Active && AutoLastHit.GetValue<KeyBind>().Active)
            {
                FollowHasLock = false;
                LasthitHasLock = true;
            }
            //disable auto last hit
            if (LasthitHasLock)
            {
                if (Utils.SleepCheck("menu0"))
                {
                    if (AutoLastHit.GetValue<KeyBind>().Active)
                    {
                        AutoLastHit.SetValue(new KeyBind(AutoLastHit.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        //FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                    }
                    Utils.Sleep(200, "menu0");
                }
                LasthitHasLock = false;
            }

            //FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, true));
            if (!AutoLastHit.GetValue<KeyBind>().Active)
            {
                if (FamiliarBeingAttackedDrawingEn)
                {
                    FamiliarBeingAttackedDrawingEn = false;
                }              
            }

            if (!AnyfamiliarNearby)
            {
                if (Utils.SleepCheck("fmove"))
                {
                    foreach (var f in familiars)
                    {
                        if (f.CanMove())
                        {
                            f.Follow(_me);
                        }
                    }
                    Utils.Sleep(100, "fmove");
                }
            }

            
        }

        private static void Game_OnUpdate_SoloKill(EventArgs args)
        {
            _me = ObjectManager.LocalHero;
            #region standard checks for loader and in-game status
            if (!_loaded)
            {
                if (!Game.IsInGame || _me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Visage)
                {
                    return;
                }
                _loaded = true;
                Console.Write("VisageSharp Loaded");
            }

            if (!Game.IsInGame || _me == null)
            {
                _loaded = false;
                return;
            }
            if (Game.IsPaused) return;
            #endregion


            var AnyfamiliarNearby = ObjectManager.GetEntities<Unit>().Any(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar
                                                                          && x.IsAlive && x.IsAlive && x.Team == _me.Team
                                                                          && x.Distance2D(_me) <= 1000);

            var _Q = _me.Spellbook.SpellQ;
            var _W = _me.Spellbook.SpellW;

            if (!Menu.Item("SoloKill").GetValue<KeyBind>().Active)
            {
                killTarget = null;
                //switch to follow mode when disable the Combo in middle of them, familiar nearby
                if (!AutoLastHit.GetValue<KeyBind>().Active && AnyfamiliarNearby 
                    && !FamiliarFollow.GetValue<KeyBind>().Active
                    && (_Q.Cooldown !=0 || _W.Cooldown != 0))
                {
                    FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, true));
                }
                return;
            }
            //disable follow mode in Combo
            VisageItems Items = new VisageItems();
            if (AutoLastHit.GetValue<KeyBind>().Active)
            {
                AutoLastHit.SetValue(new KeyBind(AutoLastHit.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
            }
            if (FamiliarFollow.GetValue<KeyBind>().Active && killTarget != null)
            {
                FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
            }
            //disable auto auto last hit if familiar is near the enemy
            if (AnyfamiliarNearby)
            {
                AutoLastHit.SetValue(new KeyBind(AutoLastHit.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));

            }

            var hasLens = _me.Inventory.Items.Any(x => x.Name == "item_aether_lens");
            //var _Q = _me.Spellbook.SpellQ;
            //var _W = _me.Spellbook.SpellW;
            var _R = _me.Spellbook.SpellR;
            
            //lockdown target
            if (killTarget == null || !killTarget.IsValid || !Menu.Item("LockTarget").GetValue<bool>())
            {
                killTarget = _me.ClosestToMouseTarget(1000);
            }
            if (killTarget == null || !killTarget.IsValid || !killTarget.IsAlive)
            {
                SoloKill.SetValue(new KeyBind(SoloKill.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                //enable the follow mode?
                FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, true));
                return;
            }

            //grave chill, birds attacking, stone, soul assumption, resummon
            //var familiarNearBy = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar
            //                                                            && x.IsAlive && x.CanAttack() && x.Team == _me.Team
            //                                                          && x.Distance2D(killTarget) <= 1200);
            //grave chill

            //Items
            Items.Medalion(killTarget);
            Items.RodOfAtos(killTarget);
            Items.SolarCrest(killTarget);

            #region grave chill

            if (_me.IsAlive && killTarget.Distance2D(_me) <= 600 + (hasLens ? 180 : 0) && _Q.CanBeCasted() && !killTarget.IsMagicImmune())
            {
                if (Utils.SleepCheck("gravechill"))
                {
                    _Q.UseAbility(killTarget);
                    Utils.Sleep(200, "gravechill");
                }               
            }
            else
            {
                if (_me.IsAlive && killTarget.Distance2D(_me) >= 600 + (hasLens ? 180 : 0) && _Q.CanBeCasted() && !killTarget.IsMagicImmune())
                {
                    if (Utils.SleepCheck("Orbwalk"))
                    {
                        _me.Move(killTarget.Position);
                        Utils.Sleep(100, "Orbwalk");
                    }
                }else if (_me.IsAlive && Orbwalking.AttackOnCooldown())
                {
                    if (_me.IsAlive && Utils.SleepCheck("Orbwalk"))
                    {
                        Orbwalking.Orbwalk(killTarget, 0, 0, false, true);
                        Utils.Sleep(100, "Orbwalk");
                    }
                }
                else
                {
                    if (_me.IsAlive && Utils.SleepCheck("Orbwalk"))
                    {
                        Orbwalking.Attack(killTarget, false);
                        Utils.Sleep(100, "Orbwalk");
                    }                 
                }
            }
            #endregion

            #region soul assumption
            var soulAssumption = _me.Modifiers.Where(x => x.Name == "modifier_visage_soul_assumption").FirstOrDefault();
            if (soulAssumption == null)
            {
                //OrbAttack
            }
            else {
                if(_me.IsAlive && killTarget.Health <= getSoulAssumptionDmg(_me, killTarget, hasLens))
                {
                    if (Utils.SleepCheck("soulassumption"))
                    {
                        if (_W.CanBeCasted() && !_me.IsInvisible())
                        {
                            _W.UseAbility(killTarget);

                        }
                        Utils.Sleep(200, "soulassumption");
                    }
                }
                else if (_me.IsAlive && soulAssumption.StackCount == 2 + _W.Level && !killTarget.IsMagicImmune() && killTarget.IsAlive
                         && !killTarget.IsIllusion && killTarget.Distance2D(_me) <= (hasLens ? 1080 : 900) + 100)
                {
                    if (Utils.SleepCheck("soulassumption"))
                    {
                        if (_W.CanBeCasted() && !_me.IsInvisible())
                        {
                            _W.UseAbility(killTarget);

                        }
                        Utils.Sleep(200, "soulassumption");
                    }
                }
            }
            #endregion

            #region familiar
            var familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);

            if (familiars.Any<Unit>(f => f.Spellbook.SpellQ.CanBeCasted() && (f.BonusDamage < 20 || f.Health <= 3))) // stone? 
            {
                _R = _me.Spellbook.SpellR;
                if (Utils.SleepCheck("fstone") && _R.Cooldown <= 200 - _R.Level * 20 - 5)
                {
                    foreach (var f in familiars)
                    {
                        if ((f.BonusDamage < 20 || f.Health <= 3) && (_R.Cooldown == 0 || _R.Cooldown <= 200 - _R.Level * 20 - 5))
                        {
                            f.Spellbook.SpellQ.UseAbility();
                        }
                        if (familiars != null)
                        {

                        }
                    }
                    Utils.Sleep(100, "fstone");
                }
            }




            var _familiarNearby = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar 
                                                                          && x.IsAlive && x.IsAlive && x.Team == _me.Team
                                                                          && x.Distance2D(killTarget) <= 1000);
            if (_familiarNearby == null)
            {
                if(_R.Cooldown != 0 || !_R.CanBeCasted())
                {
                    //no Familiars in Combo
                }
            }
            else // there is familiar around
            {
                if (Utils.SleepCheck("fattack"))
                {
                    foreach(var f in _familiarNearby)
                    {
                        if (f.CanAttack() && (f.BonusDamage > 15 || !f.Spellbook.SpellQ.CanBeCasted()))
                        {
                            f.Attack(killTarget);
                        } // no attack bonuses
                        else if(f.Distance2D(killTarget) >= 100 && f.CanMove())
                        {
                            f.Move(f.Spellbook.SpellQ.GetPrediction(killTarget));
                        }
                        else if(f.Distance2D(killTarget) <= 100 && f.Spellbook.SpellQ.CanBeCasted() && _R.Cooldown <= 200 - _R.Level * 20 - 5)
                        {
                            f.Spellbook.SpellQ.UseAbility();
                        }
                    }
                    Utils.Sleep(100, "fattack");
                }
            }
            #endregion
        }

        private static void Game_OnUpdate_AutoFamaliarLastHit(EventArgs args)
        {
            _me = ObjectManager.LocalHero;
            #region standard checks for loader and in-game status
            if (!_loaded)
            {
                if (!Game.IsInGame || _me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Visage)
                {
                    return;
                }
                _loaded = true;
                Console.Write("VisageSharp Loaded");
            }

            if (!Game.IsInGame || _me == null)
            {
                _loaded = false;
                return;
            }
            if (Game.IsPaused) return;
            #endregion

            

            if (!AutoLastHit.GetValue<KeyBind>().Active)
            {
                LasthitHasLock = true;
                return;
            }
            if (FamiliarFollow.GetValue<KeyBind>().Active && AutoLastHit.GetValue<KeyBind>().Active)
            {
                FollowHasLock = true;
                LasthitHasLock = false;
            }
            //disable another menu
            if (FollowHasLock)
            {
                if (Utils.SleepCheck("menu"))
                {
                    if (FamiliarFollow.GetValue<KeyBind>().Active)
                    {
                        FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        //FamiliarFollow.SetValue(new KeyBind(FamiliarFollow.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                    }
                    Utils.Sleep(400, "menu");
                }
                FollowHasLock = false;
            }
            //familiarAttacked = false;
            //disable follow mode

            FamiliarBeingAttackedDrawingEn = false;
            var familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);
            if (familiars == null) return;
            var _familar = familiars.FirstOrDefault();
            if(_familar == null) return;
            
            if (autoAttackMode != 0)
            {
                autoAttackMode = 0;
                Game.ExecuteCommand("dota_player_units_auto_attack_mode " + autoAttackMode);
            }

            //if key is On
            if (familiars.Any<Unit>(f => f.Spellbook.SpellQ.CanBeCasted() && (f.BonusDamage < 20 || f.Health  <= 3))) // stone? 
            {
                _R = _me.Spellbook.SpellR;
                if (Utils.SleepCheck("fstone") && _R.Cooldown <= 200 - _R.Level * 20 - 5)
                {
                    foreach (var f in familiars)
                    {
                        if ((f.BonusDamage < 20 || f.Health <= 3) && (_R.Cooldown == 0 || _R.Cooldown <= (_R.Level * 14 + 83)))
                        {
                            f.Spellbook.SpellQ.UseAbility();
                        }
                        if(familiars != null)
                        {

                        }
                    }
                    Utils.Sleep(100, "fstone");
                }
            }

            var _LowerHpCreep = GetLowestHpCreep(_familar, null, 1000); // get creeps around the 1st familiar
            //var nearestTower = ObjectManager.GetEntities<Unit>().Where(_x => _x.ClassID == ClassID.CDOTA_BaseNPC_Tower && _x.Team != _me.Team && _x.IsAlive);
            var AnyoneAttackingMe = ObjectManager.TrackingProjectiles.Any(x => x.Target.Name.Equals(_familar.Name));
            

            //if no ally creeps nearby, go follow the nearst ally creeps
            var anyAllyCreepsAround = ObjectManager.GetEntities<Unit>().Any(_x =>
                                                                              _x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                                                              && _x.IsAlive
                                                                              && (_x.Name.Equals("npc_dota_creep_badguys_melee") || _x.Name.Equals("npc_dota_creep_badguys_ranged"))
                                                                              && familiars.Any<Unit>(_y => _y.Distance2D(_x) < 300));

            if (!anyAllyCreepsAround)
            {
                // go to the closest ally creep
                var closestAllyCreep = ObjectManager.GetEntities<Unit>().Where(_x =>
                                                                          _x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                                                          && _x.IsAlive
                                                                          && _x.Name.Equals("npc_dota_creep_badguys_melee")).
                                                                          OrderBy(x => x.Distance2D(_familar)).FirstOrDefault();
                if (closestAllyCreep == null) return;
                if (Utils.SleepCheck("move"))
                {
                    foreach (var f in familiars)
                    {

                        if (f.CanMove())
                        {
                            f.Follow(closestAllyCreep);
                        }

                    }
                    Utils.Sleep(100, "move");
                }
            }
            else {

                if (AnyoneAttackingMe)//there is ally creeps
                {
                    //Console.WriteLine("someone attacking me");
                    
                    var closestAllyCreep = ObjectManager.GetEntities<Unit>().Where(_x =>
                                                                              (_x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                                                              && _x.IsAlive
                                                                              && _x.Name.Equals("npc_dota_creep_badguys_ranged"))).
                                                                              OrderBy(x => x.Distance2D(_familar)).FirstOrDefault();
                    //Console.WriteLine("found ally creeps + " + closestAllyCreeps.Name);
                    if (closestAllyCreep == null) return;
                    if (Utils.SleepCheck("move"))
                    {
                        foreach (var f in familiars)
                        {

                            if (f.CanMove())
                            {
                                f.Move(closestAllyCreep.Position);
                            }

                        }
                        Utils.Sleep(100, "move");
                    }
                }
                else {
                    var _creepTarget = KillableCreep(_familar, _LowerHpCreep);
                    if (_creepTarget == null) return;
                    //Console.WriteLine("creep target is " + _creepTarget.Health);
                    // If killable is seen
                    var _NumOfMeleeOnKillabledCreep = NumOfMeleeCreepsAttackingMe(_creepTarget);
                    var _NumOfRangeOnKillabledCreep = NumOfRangedCreepsAttackingMe(_creepTarget);
                    //Console.WriteLine("bear dmg is " + getDamageFromBear22);
                    if (_NumOfMeleeOnKillabledCreep + _NumOfRangeOnKillabledCreep != 0)
                    {
                        if (_creepTarget != null && _creepTarget.IsValid && _creepTarget.IsVisible && _creepTarget.IsAlive)
                        {
                            //Console.WriteLine("creep target is " + _creepTarget.Health);
                            var AttackableFamiliar = familiars.Where(x => x.CanAttack()
                                                                     && x.Modifiers.Any(y => y.Name == "modifier_visage_summon_familiars_damage_charge")
                                                                     && x.IsAlive
                                                                     );
                            var AttackableFamilarInRange = familiars.Where(x => x.CanAttack()
                                                                     && x.Modifiers.Any(y => y.Name == "modifier_visage_summon_familiars_damage_charge")
                                                                     && x.Distance2D(_creepTarget) <= x.AttackRange
                                                                     );
                            //use the first familiar for conditions
                            if (AttackableFamiliar == null) return;
                            if (AttackableFamiliar.All<Unit>(f => _creepTarget.Distance2D(f) <= f.AttackRange && f.CanAttack()))
                            // if(_creepTarget.Distance2D(_familar) <= _familar.AttackRange && _familar.CanAttack())
                            {
                                //Console.WriteLine("Attackeable familiar count is " + AttackableFamiliar.Count());

                                var familiarDmg = AttackableFamilarInRange.Sum(f => GetDmanageOnTargetFromSource(f, _creepTarget, 0));
                                //Console.WriteLine("famalir dmg " + familiarDmg);
                                

                                // dispatch attack when health is below threshold
                                if (_creepTarget.Health < familiarDmg)
                                {
                                    foreach (var f in AttackableFamiliar)
                                    {
                                        if (!f.IsAttacking())
                                        {
                                            f.Attack(_creepTarget);
                                        }
                                    }
                                }
                                else if (_creepTarget.Health < familiarDmg * 2 && _creepTarget.Health > familiarDmg)
                                //attack-hold
                                {
                                    if (Utils.SleepCheck("familiarAttack"))
                                    {
                                        foreach (var f in AttackableFamiliar)
                                        {
                                            f.Hold();
                                            f.Attack(_creepTarget);
                                        }
                                        Utils.Sleep(100, "familiarAttack");
                                    }
                                }
                            }
                            else
                            //lowest and killable, 
                            //other cases
                            {
                                if (AttackableFamiliar.Any<Unit>(x => x.Distance2D(_creepTarget) > x.AttackRange) && _creepTarget.ClassID != ClassID.CDOTA_BaseNPC_Creep_Siege)
                                {
                                    if (Utils.SleepCheck("familiarmove"))
                                    {
                                        foreach (var f in AttackableFamiliar)
                                        {
                                            f.Move(_LowerHpCreep.Position);
                                        }
                                        Utils.Sleep(100, "familiarmove");
                                    }

                                }
                            }
                        }
                        else // not in range
                        {
                            if (Utils.SleepCheck("move") && _creepTarget.ClassID != ClassID.CDOTA_BaseNPC_Creep_Siege)
                            {
                                foreach (var f in familiars)
                                {

                                    f.Move(_creepTarget.Position);

                                }
                                Utils.Sleep(200, "move");
                            }
                        }
                    }
                    else
                    {
                        var AttackableFamilarInRange = familiars.Where(x => x.CanAttack()
                                                                     && x.Modifiers.Any(y => y.Name == "modifier_visage_summon_familiars_damage_charge")
                                                                     && x.Distance2D(_creepTarget) <= x.AttackRange
                                                                     );
                        var familiarDmg = AttackableFamilarInRange.Sum(f => GetDmanageOnTargetFromSource(f, _creepTarget, 0));
                        if (Utils.SleepCheck("attack") && _creepTarget.Health < familiarDmg * 1.5)
                        {
                            foreach (var f in familiars)
                            {
                                if (_creepTarget.ClassID != ClassID.CDOTA_BaseNPC_Creep_Siege)
                                {
                                    f.Attack(_creepTarget);

                                }
                                Utils.Sleep(200, "attack");
                            }
                        }
                    }
                }
            }


        }

        private static void Game_OnUpdate_FamiliarControl(EventArgs args)
        {
            _me = ObjectManager.LocalHero;
            #region standard checks for loader and in-game status
            if (!_loaded)
            {
                if (!Game.IsInGame || _me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Visage)
                {
                    return;
                }
                _loaded = true;
                Console.Write("VisageSharp Loaded");
            }

            if (!Game.IsInGame || _me == null)
            {
                _loaded = false;
                return;
            }
            if (Game.IsPaused) return;
            #endregion

            var familiars = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_Unit_VisageFamiliar && x.IsAlive && x.Team == _me.Team);
            if (familiars == null) return;
            var _familiar = familiars.FirstOrDefault();
            if (_familiar == null) return;
            // auto dodge when Hero attacks
            
            var AnyRangedHeroAttackingMe = ObjectManager.TrackingProjectiles.Any(x => x.Target.Name.Equals(_familiar.Name) && ObjectManager.GetEntities<Hero>().Any(y => y.Team != _me.Team && y.Name == x.Source.Name));
            // if hero attacking Me
            //NotinComboMode
            if (!Menu.Item("SoloKill").GetValue<KeyBind>().Active) {
                if (AnyRangedHeroAttackingMe || ObjectManager.GetEntities<Hero>().Any(x => x.IsAttacking() && x.Distance2D(_familiar) <= x.AttackRange && x.Team != _me.Team && x.IsMelee))
                {
                    familiarAttacked = true;
                    // go to the closet tower and disable auto last hit
                    var ClosestAllyTower = ObjectManager.GetEntities<Unit>().Where(x => x.ClassID == ClassID.CDOTA_BaseNPC_Tower
                                                                                        && x.Team == _me.Team
                                                                                        ).OrderBy(y => y.Distance2D(_familiar))
                                                                                       .FirstOrDefault();
                    if (ClosestAllyTower == null)
                    {
                        if (Utils.SleepCheck("move"))
                        {
                            foreach (var f in familiars)
                            {

                                if (f.CanMove())
                                {
                                    f.Follow(ObjectManager.GetEntities<Unit>().Where(_x => _x.ClassID == ClassID.CDOTA_Unit_Fountain && _x.Team == _me.Team).FirstOrDefault());
                                }

                            }
                            Utils.Sleep(100, "move");
                        }
                    }
                    else
                    {
                        if (Utils.SleepCheck("move"))
                        {
                            foreach (var f in familiars)
                            {

                                if (f.CanMove())
                                {
                                    f.Follow(ClosestAllyTower);
                                }

                            }
                            Utils.Sleep(100, "move");
                        }
                        //Show Familiar Being attacked, until autolast hit is reenabled again
                        if(familiars != null) FamiliarBeingAttackedDrawingEn = true;
                        AutoLastHit.SetValue(new KeyBind(AutoLastHit.GetValue<KeyBind>().Key, KeyBindType.Toggle, false));
                        return;
                    }
                }
            }
            else
            {
                return;
            }
        }

        private static void Game_OnUpdate_Infos(EventArgs args)
        {
            _me = ObjectManager.LocalHero;
            #region standard checks for loader and in-game status
            if (!_loaded)
            {
                if (!Game.IsInGame || _me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Visage)
                {
                    return;
                }
                _loaded = true;
                Console.Write("VisageSharp Loaded");
            }

            if (!Game.IsInGame || _me == null)
            {
                _loaded = false;
                return;
            }
            if (Game.IsPaused) return;
            #endregion
        }

        private static void Game_OnUpdate_NukeOn(EventArgs args)
        {
            _me = ObjectManager.LocalHero;
            #region standard checks for loader and in-game status
            if (!_loaded)
            {
                if (!Game.IsInGame || _me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_Visage)
                {
                    return;
                }
                _loaded = true;
                Console.Write("VisageSharp Loaded");
            }

            if (!Game.IsInGame || _me == null)
            {
                _loaded = false;
                return;
            }
            if (Game.IsPaused) return;
            #endregion

            
                

            if (!Menu.Item("AutoSoulAssump").GetValue<KeyBind>().Active || !_me.IsAlive)
            {
                return;
            }

            var hasLens = _me.FindItem("item_aether_lens") != null;
            _W = _me.Spellbook.SpellW;
           

            var NearbyEnemy = ObjectManager.GetEntities<Hero>().Where(x => !x.IsMagicImmune() && x.IsAlive
                                                                           && !x.IsIllusion && x.Team != _me.Team
                                                                           && x.Distance2D(_me) <= (hasLens ? 1080 : 900) + 100);
            if (NearbyEnemy == null) return;
            var MinHpTargetNearbyEnemy = NearbyEnemy.OrderBy(x => x.Health).FirstOrDefault();
            if (MinHpTargetNearbyEnemy == null) return;
            var killableTarget = NearbyEnemy.Where(x => x.Health <= getSoulAssumptionDmg(_me, x, hasLens)).FirstOrDefault();
            if(killableTarget == null)
            {
                //max dmg to one of them
                // cast 
                var soulAssumption = _me.Modifiers.Where(x => x.Name == "modifier_visage_soul_assumption").FirstOrDefault();
                if (soulAssumption == null) return;
                //else
                if(soulAssumption.StackCount == 2 + _W.Level)
                {
                    if (Utils.SleepCheck("soulassumption"))
                    {
                        if (_W.CanBeCasted() && !_me.IsInvisible())
                        {
                            _W.UseAbility(MinHpTargetNearbyEnemy);

                        }
                        Utils.Sleep(200, "soulassumption");
                    }
                }
            }
            else
            {
                //there is killable target
                if (Utils.SleepCheck("soulassumption"))
                {
                    if (_W.CanBeCasted())
                    {
                        _W.UseAbility(killableTarget);
                        
                    }
                    Utils.Sleep(200, "soulassumption");
                }
            }


            //




        }

        private static void DrawTarget(Hero _target)
        {
            if (meToTargetParticleEffect == null)
            {
                meToTargetParticleEffect = new ParticleEffect(@"particles\ui_mouseactions\range_finder_tower_aoe.vpcf", _target);     //target inditcator
                meToTargetParticleEffect.SetControlPoint(2, new Vector3(_me.Position.X, _me.Position.Y, _me.Position.Z));             //start point XYZ
                meToTargetParticleEffect.SetControlPoint(6, new Vector3(1, 0, 0));                                                    // 1 means the particle is visible
                meToTargetParticleEffect.SetControlPoint(7, new Vector3(_target.Position.X, _target.Position.Y, _target.Position.Z)); //end point XYZ
            }
            else //updating positions
            {
                meToTargetParticleEffect.SetControlPoint(2, new Vector3(_me.Position.X, _me.Position.Y, _me.Position.Z));
                meToTargetParticleEffect.SetControlPoint(6, new Vector3(1, 0, 0));
                meToTargetParticleEffect.SetControlPoint(7, new Vector3(_target.Position.X, _target.Position.Y, _target.Position.Z));
            }
        }

        private static int NumOfMeleeCreepsAttackingMe(Unit me)
        {
            int num = 0;
            //melee creeps name = npc_dota_creep_badguys_melee
            //ranged creps name = npc_dota_creep_badguys_ranged
            try
            {
                var allMeleeCreepsAttackingMe = ObjectManager.GetEntities<Unit>().Where(_x =>
                                                                                    (_x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                                                                    && me.Distance2D(_x) <= 150
                                                                                    && _x.Team != me.Team
                                                                                    && _x.IsAlive
                                                                                    && _x.GetTurnTime(me) == 0)
                                                                                    && _x.Name.Equals("npc_dota_creep_badguys_melee"));
                if (allMeleeCreepsAttackingMe == null) return num;
                num = allMeleeCreepsAttackingMe.Count();
            }
            catch
            {

            }
            return num;
        }

        private static int NumOfRangedCreepsAttackingMe(Unit me)
        {
            int num = 0;
            //melee creeps name = npc_dota_creep_badguys_melee
            //ranged creeps name = npc_dota_creep_badguys_ranged
            try
            {
                var allRangedCreepsAttackingMe = ObjectManager.GetEntities<Unit>().Where(_x =>
                                                                                    (_x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                                                                    && me.Distance2D(_x) <= 650
                                                                                    && _x.Team != me.Team
                                                                                    && _x.IsAlive
                                                                                    && _x.GetTurnTime(me) == 0)
                                                                                    && _x.Name.Equals("npc_dota_creep_badguys_ranged"));
                if (allRangedCreepsAttackingMe == null) return num;
                num = allRangedCreepsAttackingMe.Count();             
            }
            catch
            {

            }
            return num;
        }

        private static double GetDmanageOnTargetFromSource(Unit src, Unit target, double bonusdmg)
        {
            double realDamage = 0;
            double physDamage = src.MinimumDamage + src.BonusDamage;
            if (src == null)
            {
                return realDamage;
            }
            
            if (target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
               target.ClassID == ClassID.CDOTA_BaseNPC_Tower)
            {
                realDamage = realDamage / 3;
            }

            var damageMp = 1 - 0.06 * target.Armor / (1 + 0.06 * Math.Abs(target.Armor));
            realDamage = (bonusdmg + physDamage) * damageMp;
            return realDamage;
        }

        private static Unit KillableCreep(Unit src, Unit killableCreep)
        {
            try
            {
                /*
                var time = _me.IsRanged == false
                ? _meAttackPoint * 2 / 1000 + _me.GetTurnTime(minion.Position)
                : _meAttackPoint * 2 / 1000 + _me.GetTurnTime(minion.Position) + _me.Distance2D(minion) / _myProjectileSpeed;
                */
                var percent = killableCreep.Health / killableCreep.MaximumHealth * 100;
                if (killableCreep.Health < GetDmanageOnTargetFromSource(src, killableCreep, 0) * 8 &&
                    (percent < 75 || killableCreep.Health < GetDmanageOnTargetFromSource(src, killableCreep, 0))
                    )
                {
                    return killableCreep;
                }
                
            }
            catch (Exception)
            {
                //
            }
            return null;
        }

        private static Unit GetLowestHpCreep(Unit source, Unit markedcreep, int range)
        {
            try
            {
                var lowestHp =
                    ObjectManager.GetEntities<Unit>()
                        .Where(
                            x =>
                                (x.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                                 x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Additive
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Barracks
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Building
                                 || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible
                                && x.Team != source.Team && x.Distance2D(source) < range &&
                                x != markedcreep
                                && x.Health/x.MaximumHealth < 0.9)
                        .OrderBy(creep => creep.Health)
                        .DefaultIfEmpty(null)
                        .FirstOrDefault();
                return lowestHp;
            }
            catch (Exception)
            {
                //
            }
            return null;
        }

        private static double getSoulAssumptionDmg(Hero me, Hero target, bool lens)
        {
            double realdmg = 0;
            if (target == null) return 0;
            var W = me.Spellbook.SpellW;
            if (W == null) return 0;

            var soulAssumption = _me.Modifiers.Where(x => x.Name == "modifier_visage_soul_assumption");
            if (soulAssumption == null) return 0;


            var stackCount = soulAssumption.FirstOrDefault().StackCount;
            var magicResist = target.MagicDamageResist;
            var magicDmg = 20 + stackCount * 110;
            realdmg = magicDmg * (1 - magicResist) * ((lens == true)? 1.08 : 1);
            return realdmg; 
        }

    }
}
