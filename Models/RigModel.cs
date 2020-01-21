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
                    rvs[i] = null;
                    rvs.RemoveAt(i);
                    break;
                }
            }
            RVs = (from r in rvs orderby r.Index select r).ToList();
            for (int i = 0; i < rvms.Count; i++)
            {
                if (rvms[i].Index == RigIndex)
                {
                    rvms[i] = null;
                    rvms.RemoveAt(i);
                    break;
                }
            }
            RVMs = (from r in rvms orderby r.Index select r).ToList();
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