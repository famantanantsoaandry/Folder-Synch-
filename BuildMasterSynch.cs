using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace BuildMaster_Backup
{
    [RunInstaller(true)]
    public partial class BuildMasterSynch : System.Configuration.Install.Installer
    {
        public BuildMasterSynch()
        {
            InitializeComponent();
        }

        private void ServiceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
