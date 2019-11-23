using OMineWatcher.Managers;
using OMineWatcher.Views;
using System.Collections.Generic;
using System.Linq;

namespace OMineWatcher.ViewModels
{
    public partial class MainViewModel
    {
        public List<RigView> RVs { get; set; } = new List<RigView>();
        private List<RigViewModel> RVMs { get; set; } = new List<RigViewModel>();
        private void AddRigPanel(int RigIndex)
        {
            List<RigView> rvs = RVs;
            List<RigViewModel> rvms = RVMs;

            RigViewModel RVM = new RigViewModel(this, RigIndex);
            rvms.Add(RVM);
            RVMs = (from rvm in rvms orderby rvm.Index select rvm).ToList();

            RigView RV = new RigView(RVM, RigIndex);
            rvs.Add(RV);
            RVs = (from rv in rvs orderby rv.Index select rv).ToList();
        }
        private void RemoveRigPanel(int RigIndex)
        {
            List<RigView> rvs = RVs;
            List<RigViewModel> rvms = RVMs;

            for (int i = 0; i < rvs.Count; i++)
            {
                if (rvs[i].Index == RigIndex)
                {
                    rvs.RemoveAt(i);
                    break;
                }
            }
            RVs = (from r in rvs orderby r.Index select r).ToList();
            for (int i = 0; i < rvms.Count; i++)
            {
                if (rvms[i].Index == RigIndex)
                {
                    rvms.RemoveAt(i);
                    break;
                }
            }
            RVMs = (from r in rvms orderby r.Index select r).ToList();
        }
        private void RigInform()
        {
            OMG_TCP.RootObject ro = _model.RigInform.RO;
            foreach (RigViewModel rvm in RVMs)
            {
                if (_model.RigInform.Index == rvm.Index)
                {
                    if (ro.Indication != null)
                    {
                        rvm.SetIndicator((bool)ro.Indication);
                    }
                    if (ro.Hashrates != null)
                    {
                        rvm.Hashrates = ro.Hashrates;
                        rvm.Totalhashrate = ro.Hashrates.Sum();
                        rvm.SetIndicator(true);
                    }
                    if (ro.Temperatures != null)
                    {
                        rvm.Temperatures = ro.Temperatures;
                        rvm.TotalTemperature = ro.Temperatures.Max();
                    }

                    if (ro.RigInactive != null)
                    {
                        rvm.SetIndicator(false);
                        rvm.Hashrates = null;
                        rvm.Totalhashrate = null;
                        rvm.Temperatures = null;
                        rvm.TotalTemperature = null;
                    }
                }
            }
        }
        private void SetBaseMaxTemp(int t)
        {
            foreach (RigViewModel rvm in RVMs)
            {
                rvm.SetBaseMaxTemp(t);
            }
        }
        private void SetBaseMinTemp(int t)
        {
            foreach (RigViewModel rvm in RVMs)
            {
                rvm.SetBaseMinTemp(t);
            }
        }
        private void SetCurrentMaxTemp(int i, int t)
        {
            foreach (RigViewModel rvm in RVMs)
            {
                if (Rigs[i].Index == rvm.Index)
                {
                    rvm.MaxTempCurr = t;
                }
            }
        }
        private void SetCurrentMinTemp(int i, int t)
        {
            foreach (RigViewModel rvm in RVMs)
            {
                if (Rigs[i].Index == rvm.Index)
                {
                    rvm.MinTempCurr = t;
                }
            }
        }
    }
}