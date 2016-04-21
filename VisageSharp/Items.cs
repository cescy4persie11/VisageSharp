using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage.Items;
using Ensage.Common.Extensions;
using Ensage.Common;
using Ensage;

namespace VisageSharp
{
    
    class VisageItems
    {
        public void Medalion(Unit _target)
        {
            var _me = ObjectManager.LocalHero;
            Item medalion = _me.FindItem("item_medallion_of_courage");
            if (_me.Inventory.Items.Any(x => x.Name == "item_medallion_of_courage"))
            {
              if(_me.IsAlive && !_target.IsMagicImmune() && !_me.IsInvisible() 
                    && _target.Distance2D(_me) <= 1000 + 100 && medalion.CanBeCasted())
                {
                    if (Utils.SleepCheck("medalion"))
                    {
                        medalion.UseAbility(_target);
                        Utils.Sleep(100, "medalion");
                    }
                }
            }
            else
            {
                return;
            }

        }

        public void SolarCrest(Unit _target)
        {
            var _me = ObjectManager.LocalHero;
            Item SolarCrest = _me.FindItem("item_solar_crest");
            if (_me.Inventory.Items.Any(x => x.Name == "item_solar_crest"))
            {
                if (_me.IsAlive && !_target.IsMagicImmune() && !_me.IsInvisible()
                      && _target.Distance2D(_me) <= SolarCrest.CastRange + 100 && SolarCrest.CanBeCasted())
                {
                    if (Utils.SleepCheck("solarcrest"))
                    {
                        SolarCrest.UseAbility(_target);
                        Utils.Sleep(100, "solarcrest");
                    }
                }
            }
            else
            {
                return;
            }
        }

        public void RodOfAtos(Unit _target)
        {
            var _me = ObjectManager.LocalHero;
            Item Rod = _me.FindItem("item_rod_of_atos");
            if (_me.Inventory.Items.Any(x => x.Name == "item_rod_of_atos"))
            {
                if (_me.IsAlive && !_target.IsMagicImmune() && !_me.IsInvisible()
                      && _target.Distance2D(_me) <= Rod.CastRange + 100 && Rod.CanBeCasted())
                {
                    if (Utils.SleepCheck("rod"))
                    {
                        Rod.UseAbility(_target);
                        Utils.Sleep(100, "rod");
                    }
                }
            }
            else
            {
                return;
            }
        }
    }
}
