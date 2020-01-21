using OMineWatcher.Views;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OMineWatcher.ViewModels
{
    public partial class MainViewModel
    {
        public List<PoolView> PVs { get; set; } = new List<PoolView>();
        private List<PoolViewModel> PVMs { get; set; } = new List<PoolViewModel>();

        public void WachPoolStart(int index)
        {
            List<PoolView> pvs = PVs;
            List<PoolViewModel> pvms = PVMs;

            PoolViewModel PVM = new PoolViewModel(PoolsSets[index], index);
            pvms.Add(PVM);
            PVMs = (from pvm in pvms orderby pvm.Index select pvm).ToList();

            PoolView PV = new PoolView(PVM, index);
            pvs.Add(PV);
            PVs = (from pv in pvs orderby pv.Index select pv).ToList();
        }
        public void WachPoolStop(int index)
        {
            List<PoolView> pvs = PVs;
            List<PoolViewModel> pvms = PVMs;

            for (int i = 0; i < pvs.Count; i++)
            {
                if (pvs[i].Index == index)
                {
                    pvs[i] = null;
                    pvs.RemoveAt(i);
                    break;
                }
            }
            PVs = (from p in pvs orderby p.Index select p).ToList();
            for (int i = 0; i < pvms.Count; i++)
            {
                if (pvms[i].Index == index)
                {
                    pvms[i] = null;
                    pvms.RemoveAt(i);
                    break;
                }
            }
            PVMs = (from p in pvms orderby p.Index select p).ToList();
        }

        public void SortPoolsViews()
        {
            PVs = PVs.OrderBy(pv => pv.Index).ToList();
        }
    }
}
