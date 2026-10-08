using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using WiimoteGun;
using WiimoteGun.Common;
using WiimoteGun.Core;

namespace WiimoteGun.Controls
{
    public partial class OptionsControl : UserControl
    {
        // [V54] Tooltips for the Gestures checkboxes (EN/FR: Bulles des cases Gestures)
        private ToolTip _toolTips = new ToolTip();

        // [V57d] UMDF2 selection-time activation flow state
        // (EN/FR: État du flux d'activation UMDF2 au moment de la sélection)
        private bool _hmActivationFlowActive;   // reentrancy guard / garde de réentrance
        private bool _hmActivatedThisSession;   // activated at selection time / activé dès la sélection

        public OptionsControl()
        {
            InitializeComponent();
            // [V57o3] EN: UiScaler owns ALL scaling on this page: with the Designer's AutoScaleMode.Font,
            //     changing the page's root font (ApplyFonts) made WinForms RE-SCALE the children
            //     on top of our own Scale (double-scale). None disables that interference.
            //     FR: UiScaler possede TOUT le scaling de cette page : avec l'AutoScaleMode.Font
            //     du Designer, le changement de police racine (ApplyFonts) faisait RE-SCALER les
            //     enfants par WinForms PAR-DESSUS notre Scale (double-scale). None supprime cette
            //     interference.
            this.AutoScaleMode = AutoScaleMode.None;
            BindEvents();
            LoadOptionsFromInstance();

            // [V57d] EN: Propose the UMDF2 install IMMEDIATELY when the mode is selected
            //     (user request), not only at Apply time. Wired AFTER the initial load so
            //     the programmatic selection does not trigger the flow.
            //     FR: Proposer l'installation UMDF2 IMMÉDIATEMENT à la sélection du mode
            //     (demande utilisateur), pas seulement au Apply. Câblé APRÈS le chargement
            //     initial pour que la sélection programmatique ne déclenche pas le flux.
            optMouseMode.SelectedIndexChanged += OptMouseMode_SelectedIndexChanged;

            // [V57d] EN: Tooltip for the input mode dropdown (General tab)
            //     FR: Bulle d'aide pour le mode d'input (onglet Général)
            _toolTips.SetToolTip(optMouseMode,
                "INPUT MODE:\r\n" +
                "- SendInput: legacy single-player injection (merged input).\r\n" +
                "- RawInput (VMulti): multi-player via the vmulti a/b/c/d drivers.\r\n" +
                "- RawInput (UMDF2): multi-player via the UMDF2/HIDMaestro host - no signed\r\n" +
                "  kernel driver, no test mode, no UAC (installed silently by the service).\r\n" +
                "Each player keeps the SAME virtual mouse+keyboard across sessions (P1..P4).\r\n" +
                "Takes effect on the next Wiimote connection / app restart.\r\n" +
                "\r\n" +
                "MODE D'INPUT :\r\n" +
                "- SendInput : injection legacy mono-joueur (flux fusionné).\r\n" +
                "- RawInput (VMulti) : multi-joueur via les pilotes vmulti a/b/c/d.\r\n" +
                "- RawInput (UMDF2) : multi-joueur via l'hôte UMDF2/HIDMaestro - aucun pilote\r\n" +
                "  kernel signé, aucun test mode, aucun UAC (installé silencieusement par le service).\r\n" +
                "Chaque joueur garde LA MÊME souris+clavier virtuels entre les sessions (P1..P4).\r\n" +
                "Prend effet à la prochaine connexion de Wiimote / relance de l'app.");

            // [V54] Explanatory tooltips for the two Off-Screen Reload options
            // (EN/FR: Bulles d'explication pour les deux options de rechargement hors-écran)
            _toolTips.SetToolTip(optEnableOffScreenReload,
                "OFF-SCREEN RELOAD (global):\r\n" +
                "Aiming OFF-screen and pulling the trigger (or the reload button) sends the\r\n" +
                "RELOAD action (right-click) instead of a shot.\r\n" +
                "All mouse clicks are locked while off-screen, and the shot is suppressed until\r\n" +
                "the trigger is released back on-screen.\r\n" +
                "Useful when the game offers no off-screen reload and no dedicated reload button.\r\n" +
                "\r\n" +
                "RECHARGEMENT HORS-ÉCRAN (global) :\r\n" +
                "Viser hors écran et presser la gâchette (ou le bouton reload) envoie l'action\r\n" +
                "RECHARGE (clic droit) au lieu d'un tir.\r\n" +
                "Tous les clics sont verrouillés hors écran ; le tir est supprimé jusqu'au\r\n" +
                "relâchement de la gâchette de retour à l'écran.\r\n" +
                "Pratique si le jeu ne propose aucun rechargement hors écran ni bouton dédié.");
            _toolTips.SetToolTip(optOffScreenReloadAuto,
                "AUTO OFF-SCREEN GLOBAL (requires Off-Screen Reload):\r\n" +
                "Going OFF-screen triggers ONE automatic reload (right-click) - no button needed.\r\n" +
                "Only ONE auto-reload per off-screen session: aim at the screen again to re-arm it.\r\n" +
                "[V55] Each mapping profile can now individually override this with a Trigger/Auto\r\n" +
                "dropdown on the mapping page. TC Cover mode inhibits both functions per profile.\r\n" +
                "\r\n" +
                "AUTO HORS-ÉCRAN GLOBAL (requiert Off-Screen Reload) :\r\n" +
                "Sortir de l'écran déclenche UNE recharge automatique (clic droit) - sans bouton.\r\n" +
                "Une seule recharge par session hors écran : viser à nouveau l'écran pour réarmer.\r\n" +
                "[V55] Chaque profil peut maintenant surcharger ce réglage avec une liste Trigger/Auto\r\n" +
                "sur sa page de mapping. Le mode planque TC inhibe les deux fonctions par profil.");

            // [V55y] Reload rumble tooltips (EN/FR: Bulles vibration rechargement)
            _toolTips.SetToolTip(chkReloadRumble,
                "RELOAD RUMBLE [V55y] (global):\r\n" +
                "Plays a configurable rumble pattern on the Wiimote whenever a RELOAD occurs:\r\n" +
                "- Off-Screen Auto sequence or trigger redirect (if Off-Screen Reload is enabled),\r\n" +
                "- Physical reload button press (right-click mapping) or Shake reload,\r\n" +
                "- GamePad off-screen reloads.\r\n" +
                "Works even when Off-Screen Reload is disabled.\r\n" +
                "Each mapping profile (Mouse AND GamePad pages) can override this setting.\r\n" +
                "\r\n" +
                "VIBRATION RECHARGEMENT [V55y] (global) :\r\n" +
                "Joue un motif de vibration paramétrable sur la Wiimote à chaque RECHARGE :\r\n" +
                "- Séquence Auto hors écran ou redirection gâchette (si Off-Screen Reload activé),\r\n" +
                "- Appui du bouton reload physique (mapping clic droit) ou shake reload,\r\n" +
                "- Recharges hors écran en mode GamePad.\r\n" +
                "Fonctionne même si le Off-Screen Reload est désactivé.\r\n" +
                "Chaque profil de mapping (pages Souris ET GamePad) peut surcharger ce réglage.");
            _toolTips.SetToolTip(trkReloadRumbleIntensity,
                "Rumble intensity 0-100%: maps to a 0.25x..3x multiplier on the ON pulses\r\n" +
                "(the Wiimote motor is binary - intensity is achieved via pulse width).\r\n" +
                "The whole range is perceptible and 100% is clearly the maximum.\r\n" +
                "FR : Intensité 0-100% : correspond à un multiplicateur 0,25x..3x sur les impulsions ON\r\n" +
                "(le moteur de la Wiimote est binaire - l'intensité passe par la largeur d'impulsion).\r\n" +
                "Toute la plage est perceptible et 100 % est clairement le maximum.");
            _toolTips.SetToolTip(cboReloadRumbleStyle,
                "Rumble style:\r\n" +
                "- Ratchet (mechanical): short clicks then a chunk - weapon reload feel.\r\n" +
                "- Short: single short pulse.\r\n" +
                "- Long: single continuous pulse.\r\n" +
                "- Custom: build your own pattern of tics (Ticks / Tic ON / Tic OFF below).\r\n" +
                "FR : Style de vibration :\r\n" +
                "- Ratchet (mécanique) : petits clics puis une taloche - sensation de rechargement d'arme.\r\n" +
                "- Court : impulsion unique courte.\r\n" +
                "- Long : impulsion unique continue.\r\n" +
                "- Personnalisé : créez votre propre motif de tics (Ticks / Tic ON / Tic OFF ci-dessous).");

            // [V55z] Custom style row tooltips (EN/FR: Bulles des paramètres du style personnalisé)
            _toolTips.SetToolTip(trkReloadRumbleTicks,
                "CUSTOM STYLE - Number of tics (1-10): the horizontal graduation is one tic per step.\r\n" +
                "Each tic plays ON then OFF using the durations below.\r\n" +
                "FR : STYLE PERSONNALISÉ - Nombre de tics (1-10) : la graduation horizontale est un tic par cran.\r\n" +
                "Chaque tic joue ON puis OFF selon les durées ci-dessous.");
            _toolTips.SetToolTip(nudReloadRumbleTickOnMs,
                "CUSTOM STYLE - ON duration of each tic in ms (10-500).\r\n" +
                "FR : STYLE PERSONNALISÉ - Durée ON de chaque tic en ms (10-500).");
            _toolTips.SetToolTip(nudReloadRumbleTickOffMs,
                "CUSTOM STYLE - OFF gap between tics in ms (10-500).\r\n" +
                "FR : STYLE PERSONNALISÉ - Écart OFF entre les tics en ms (10-500).");

            // [V55z] Show the Custom rows only when the Custom style is selected
            // (EN/FR: Afficher les lignes Custom seulement si le style Custom est sélectionné)
            cboReloadRumbleStyle.SelectedIndexChanged += (s, e) => UpdateReloadRumbleCustomUi();

            // Default tab
            SwitchTab(btnTabGeneral, 0);

            // Hide tabs at runtime (Developer Note: Tabs are visible in Designer for editing)
            this.tabsOptions.Appearance = TabAppearance.FlatButtons;
            this.tabsOptions.ItemSize = new Size(0, 1);
            this.tabsOptions.SizeMode = TabSizeMode.Fixed;

            // [V55o] Lock Mode tooltips (controls are now instantiated in Designer for VS visual preview)
            // (EN/FR: Bulles d'aide Lock Mode - les contrôles sont instanciés dans le Designer pour prévisuel VS [V55o])
            _toolTips.SetToolTip(optLockModeOnGameStart,
                "LOCK MODE AT GAME-START [V55]:\r\n" +
                "When a game launches from EmulationStation, the current WiimoteGun mode\r\n" +
                "(Mouse/GamePad) is locked. The Home button cannot cycle modes while playing.\r\n" +
                "The mode is automatically unlocked when the game ends.\r\n" +
                "\r\n" +
                "VERROUILLAGE MODE AU GAME-START [V55] :\r\n" +
                "Au lancement d'un jeu depuis EmulationStation, le mode WiimoteGun actuel\r\n" +
                "(Souris/GamePad) est verrouillé. Le bouton Home ne peut pas changer le mode\r\n" +
                "en jouant. Le mode est automatiquement déverrouillé au game-end.");

            // [V55] BT Auto-Reset tooltips (controls are now instantiated in Designer for VS visual preview)
            // (EN/FR: Bulles d'aide BT Auto-Reset - les contrôles sont instanciés dans le Designer pour prévisuel VS [V55i])
            _toolTips.SetToolTip(optAutoBtReset,
                "BT AUTO-RESET VIA SERVICE [V55]:\r\n" +
                "If no Wiimote connects within the delay below, the Helper Service\r\n" +
                "resets the Windows Bluetooth adapter (disable then re-enable).\r\n" +
                "Requires WiimoteGun Helper Service. 30-second cooldown per reset.\r\n" +
                "\r\n" +
                "BT AUTO-RESET VIA SERVICE [V55] :\r\n" +
                "Si aucune Wiimote ne se connecte dans le délai ci-dessous, le Service Helper\r\n" +
                "réinitialise l'adaptateur Bluetooth Windows (disable puis re-enable).\r\n" +
                "Nécessite le service WiimoteGun Helper. Cooldown 30s entre chaque reset.");
            _toolTips.SetToolTip(numBtResetDelay,
                "Delay in seconds before the BT reset is triggered (10–300s, default 60s).\r\n" +
                "FR : Délai en secondes avant le reset BT (10–300s, défaut 60s).");
            // EN/FR: Synchronize all options (now that all controls, including dynamic ones, are fully instantiated)
            LoadOptionsFromInstance();
        }

        private void LoadEsScriptsState()
        {
            if (chkEsScripts == null) return;
            chkEsScripts.Checked = Options.Instance.EsScriptsEnabled;
            chkEsTileHotkey.Checked = Options.Instance.EsTileHotkeyEnabled;
            numEsTileDelay.Value = Math.Min(Math.Max(Options.Instance.EsTileHotkeyDelayMs / 1000, (int)numEsTileDelay.Minimum), (int)numEsTileDelay.Maximum);
            if (optLockModeOnGameStart != null)
                optLockModeOnGameStart.Checked = Options.Instance.LockModeOnGameStart;
            UpdateEsScriptsStatus();
        }

        private void UpdateEsScriptsStatus()
        {
            if (lblEsScriptsStatus == null) return;
            try
            {
                if (string.IsNullOrEmpty(EsScriptIntegration.GetEsScriptsRoot()))
                {
                    lblEsScriptsStatus.Text = "RetroBat not found in registry (scripts unavailable)";
                    lblEsScriptsStatus.ForeColor = Color.FromArgb(220, 160, 80);
                    return;
                }
                bool valid = EsScriptIntegration.AreScriptsValid();
                lblEsScriptsStatus.Text = valid
                    ? "Scripts installed: " + EsScriptIntegration.GameStartScriptPath
                    : "Scripts missing or outdated (installed at next startup)";
                lblEsScriptsStatus.ForeColor = valid ? Color.FromArgb(120, 200, 120) : Color.FromArgb(220, 160, 80);
            }
            catch (Exception ex)
            {
                lblEsScriptsStatus.Text = "Status unavailable: " + ex.Message;
                lblEsScriptsStatus.ForeColor = Color.FromArgb(220, 120, 120);
            }
        }

        private void BindEvents()
        {
            // Tab Switching
            // Tab Switching
            btnTabGeneral.Click += (s, e) => SwitchTab(btnTabGeneral, 0);
            btnTabDetection.Click += (s, e) => SwitchTab(btnTabDetection, 1);
            btnTabGestures.Click += (s, e) => SwitchTab(btnTabGestures, 2);
            btnTabEmulators.Click += (s, e) => SwitchTab(btnTabEmulators, 3);
            btnTabEsScripts.Click += (s, e) => SwitchTab(btnTabEsScripts, 4);


            // Button Actions
            btnApply.Click += BtnApplyOptions_Click;
            btnReset.Click += (s, e) => LoadOptionsFromInstance();
            
            // Hover Effects for Sidebar
            SetupHoverEffect(btnTabGeneral);

            SetupHoverEffect(btnTabDetection);
            SetupHoverEffect(btnTabGestures);
            SetupHoverEffect(btnTabEmulators);
            SetupHoverEffect(btnTabEsScripts);

            // Back
            if (btnBack != null)
                btnBack.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

            // Standalone Browsing Buttons
            btnBrowsePCSX2.Click += (s, e) => BrowseFolder(txtPCSX2Path, "Select PCSX2 Emulators Root Folder");
            btnBrowseDuckStation.Click += (s, e) => BrowseFolder(txtDuckStationPath, "Select DuckStation Emulators Root Folder");
            btnBrowseDolphin.Click += (s, e) => BrowseFolder(txtDolphinPath, "Select Dolphin Emulators Root Folder");
            btnBrowseCemu.Click += (s, e) => BrowseFolder(txtCemuPath, "Select Cemu Emulators Root Folder");
            
            // Standalone Toggle Logic
            optStandaloneMode.CheckedChanged += (s, e) => UpdateStandaloneUIState();

            // Mutual Exclusivity for Gestures (EN/FR: Exclusivité mutuelle pour les gestes)
            optShakeDevice.SelectedIndexChanged += (s, e) => EnsureGestureDeviceExclusivity(true);
            optGrenadeDevice.SelectedIndexChanged += (s, e) => EnsureGestureDeviceExclusivity(false);
        }

        private void EnsureGestureDeviceExclusivity(bool shakeChanged)
        {
            // Only enforce if both are enabled or we want to prevent same-device mapping
            // Index 0 = Wiimote, 1 = Nunchuk (EN/FR: Index 0 = Wiimote, 1 = Nunchuk)
            if (optShakeDevice.SelectedIndex == -1 || optGrenadeDevice.SelectedIndex == -1) return;

            if (optShakeDevice.SelectedIndex == optGrenadeDevice.SelectedIndex)
            {
                // If they match, swap the one that WASN'T just changed by the user (or the other one)
                // (EN/FR: Si identique, changer celui qui n'a pas été modifié par l'utilisateur)
                if (shakeChanged)
                    optGrenadeDevice.SelectedIndex = (optShakeDevice.SelectedIndex == 0) ? 1 : 0;
                else
                    optShakeDevice.SelectedIndex = (optGrenadeDevice.SelectedIndex == 0) ? 1 : 0;
            }
        }

        private void BrowseFolder(TextBox targetTextBox, string description)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = description;
                fbd.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(targetTextBox.Text) && System.IO.Directory.Exists(targetTextBox.Text))
                    fbd.SelectedPath = targetTextBox.Text;

                if (fbd.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    targetTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        public event EventHandler BackRequested;

        private void SetupHoverEffect(Button btn)
        {
            btn.MouseEnter += (s, e) => 
            {
                if (btn.BackColor != Color.FromArgb(0, 122, 204)) // Not selected
                    btn.BackColor = Color.FromArgb(60, 60, 60);
            };
            btn.MouseLeave += (s, e) => 
            {
                // If this is the active tab, keep blue, else dark
                int index = -1;
                if (btn == btnTabGeneral) index = 0;
                else if (btn == btnTabDetection) index = 1;
                else if (btn == btnTabGestures) index = 2;
                else if (btn == btnTabEmulators) index = 3;
                else if (btn == btnTabEsScripts) index = 4;


                if (tabsOptions.SelectedIndex == index)
                    btn.BackColor = Color.FromArgb(0, 122, 204);
                else
                    btn.BackColor = Color.FromArgb(40, 40, 40);
            };
        }

        private void SwitchTab(Button activeBtn, int index)
        {
            tabsOptions.SelectedIndex = index;
            
            // Reset colors
            Color dark = Color.FromArgb(40, 40, 40);
            Color active = Color.FromArgb(0, 122, 204);
            
            btnTabGeneral.BackColor = dark;
            btnTabDetection.BackColor = dark;
            btnTabGestures.BackColor = dark;
            btnTabEmulators.BackColor = dark;
            if (btnTabEsScripts != null) btnTabEsScripts.BackColor = dark; // [V42]

            
            // Set active
            // Set active
            activeBtn.BackColor = active;

            // EN: Refresh gesture lock state when switching to Gestures tab
            // FR: Rafraîchir l'état de verrouillage des gestes lors du passage à l'onglet Gestes
            if (index == 2)
                UpdateGestureLockState();

            // [V42] Refresh ES scripts status when switching to the ES Scripts tab
            // (EN/FR: Rafraîchir le statut des scripts ES au passage sur l'onglet ES Scripts)
            if (index == 4)
                UpdateEsScriptsStatus();
        }

        public void LoadOptionsFromInstance()
        {
            // General
            optMouseMode.SelectedItem = MouseModeToLabel(Options.Instance.DefaultMouseMode);
            optMonitorId.Value = Math.Min(Math.Max(Options.Instance.MonitorId, optMonitorId.Minimum), optMonitorId.Maximum);
            optLEDLayout.SelectedItem = GetLEDLayoutName(Options.Instance.LEDLayout);
            optIRSensitivity.Value = Math.Min(Math.Max(Options.Instance.IRSensitivity, optIRSensitivity.Minimum), optIRSensitivity.Maximum);
            optShowNotifications.Checked = Options.Instance.ShowNotifications;
            optEnableGamePadSwap.Checked = Options.Instance.EnableGamePadSwapMode;
            optPersistentGamePads.Checked = Options.Instance.PersistentGamePads;
            optEnableFPSMode.Checked = Options.Instance.EnableFPSMode;
            if (optLockModeOnGameStart != null)
                optLockModeOnGameStart.Checked = Options.Instance.LockModeOnGameStart;
            if (optAutoBtReset != null)
                optAutoBtReset.Checked = Options.Instance.AutoBtResetOnFail;
            if (numBtResetDelay != null)
                numBtResetDelay.Value = Math.Min(Math.Max(Options.Instance.AutoBtResetDelaySeconds, (int)numBtResetDelay.Minimum), (int)numBtResetDelay.Maximum);

            // Log Level
            optLogLevel.SelectedItem = Options.Instance.LoggingLevel.ToString();

            // Auto-Start
            optAutoStart.Items.Clear();
            optAutoStart.Items.Add("None");
            optAutoStart.Items.Add("Windows Startup");
            optAutoStart.Items.Add("RetroBat Startup");
            optAutoStart.SelectedIndex = (int)Options.Instance.AutoStart;


            // Detection
            optDetectDolphin.Checked = Options.Instance.DetectDolphinbar;
            optDetectBluetooth.Checked = Options.Instance.DetectBlueTooth;

            // Gestures (EN/FR: Gestes)
            optEnableOffScreenReload.Checked = Options.Instance.EnableOffScreenReload;
            optOffScreenReloadAuto.Checked = Options.Instance.OffScreenReloadAuto;
            optEnableShakeReload.Checked = Options.Instance.EnableShakeReload;

            // [V55y] Reload rumble (EN/FR: Vibration rechargement)
            chkReloadRumble.Checked = Options.Instance.ReloadRumbleEnabled;
            trkReloadRumbleIntensity.Value = Math.Min(Math.Max(Options.Instance.ReloadRumbleIntensity, trkReloadRumbleIntensity.Minimum), trkReloadRumbleIntensity.Maximum);
            if (cboReloadRumbleStyle.Items.Count == 0)
            {
                cboReloadRumbleStyle.Items.AddRange(new object[] { "Ratchet (mechanical)", "Short", "Long", "Custom" });
            }
            cboReloadRumbleStyle.SelectedIndex = Math.Min(Math.Max(Options.Instance.ReloadRumbleStyle, 0), 3);

            // [V55z] Custom style parameters (EN/FR: Paramètres du style personnalisé)
            trkReloadRumbleTicks.Value = Math.Min(Math.Max(Options.Instance.ReloadRumbleCustomTicks, trkReloadRumbleTicks.Minimum), trkReloadRumbleTicks.Maximum);
            nudReloadRumbleTickOnMs.Value = Math.Min(Math.Max(Options.Instance.ReloadRumbleCustomOnMs, (int)nudReloadRumbleTickOnMs.Minimum), (int)nudReloadRumbleTickOnMs.Maximum);
            nudReloadRumbleTickOffMs.Value = Math.Min(Math.Max(Options.Instance.ReloadRumbleCustomOffMs, (int)nudReloadRumbleTickOffMs.Minimum), (int)nudReloadRumbleTickOffMs.Maximum);
            UpdateReloadRumbleCustomUi();
            
            // Map 0-3 index to Very Low, Low, Medium, High (EN/FR: Index 0-3 vers les niveaux de sensibilité)
            optShakeSensitivity.SelectedIndex = Math.Min(Math.Max(Options.Instance.ShakeSensitivity, 0), 3);
            
            // Map bool to index: 0=Wiimote, 1=Nunchuk (EN/FR: 0=Wiimote, 1=Nunchuk)
            optShakeDevice.SelectedIndex = Options.Instance.ShakeFromNunchuk ? 1 : 0;
            optGrenadeDevice.SelectedIndex = Options.Instance.GrenadeFromNunchuk ? 1 : 0;
            
            optEnableGrenadeGesture.Checked = Options.Instance.EnableGrenadeGesture;

            // Emulators
            optRestartOnDolphin.Checked = Options.Instance.RestartOnDolphin;
            optRestartOnCemu.Checked = Options.Instance.RestartOnCemu;

            // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
            optEnableIRSmoothing.Checked = Options.Instance.EnableIRSmoothing;
            optIRSmoothingStrength.Value = Math.Min(Math.Max(Options.Instance.IRSmoothingStrength, optIRSmoothingStrength.Minimum), optIRSmoothingStrength.Maximum);
            optUseHighPerfTimers.Checked = Options.Instance.UseHighPerfTimers;
            optEnableHomographyCache.Checked = Options.Instance.EnableHomographyCache;
            optEnableDistanceCompensation.Checked = Options.Instance.EnableDistanceCompensation;
            optUseIRExtrapolation.Checked = Options.Instance.UseIRExtrapolation;
            optIRExtrapolationStrength.Value = (decimal)Math.Min(Math.Max(Options.Instance.IRExtrapolationStrength, (float)optIRExtrapolationStrength.Minimum), (float)optIRExtrapolationStrength.Maximum);
            optEnableVirtualPolling.Checked = Options.Instance.EnableVirtualPolling;
            optVirtualPollingRate.Value = Math.Min(Math.Max(Options.Instance.VirtualPollingRate, (int)optVirtualPollingRate.Minimum), (int)optVirtualPollingRate.Maximum);
            optIRSmoothingStrengthV2.Value = Math.Min(Math.Max(Options.Instance.IRSmoothingStrengthV2, (int)optIRSmoothingStrengthV2.Minimum), (int)optIRSmoothingStrengthV2.Maximum);
            optIRExtrapolationStrengthV2.Value = (decimal)Math.Min(Math.Max(Options.Instance.IRExtrapolationStrengthV2, (float)optIRExtrapolationStrengthV2.Minimum), (float)optIRExtrapolationStrengthV2.Maximum);
            optVirtualPollingRateV2.Value = Math.Min(Math.Max(Options.Instance.VirtualPollingRateV2, (int)optVirtualPollingRateV2.Minimum), (int)optVirtualPollingRateV2.Maximum);

            // Standalone
            optStandaloneMode.Checked = Options.Instance.StandaloneMode;
            txtPCSX2Path.Text = Options.Instance.PCSX2Path;
            txtDuckStationPath.Text = Options.Instance.DuckStationPath;
            txtDolphinPath.Text = Options.Instance.DolphinPath;
            txtCemuPath.Text = Options.Instance.CemuPath;

            UpdateStandaloneUIState();

            // EN: Check if remap profiles have Shake mappings that override gesture settings
            // FR: Vérifier si les profils remap ont des mappings Shake qui priment sur les paramètres de gestes
            UpdateGestureLockState();

            // [V42] ES Scripts tab state (EN/FR: État de l'onglet Scripts ES)
            LoadEsScriptsState();
        }

        private void UpdateStandaloneUIState()
        {
            bool isStandalone = optStandaloneMode.Checked;

            // Enable/Disable controls
            txtPCSX2Path.Enabled = isStandalone;
            btnBrowsePCSX2.Enabled = isStandalone;
            txtDuckStationPath.Enabled = isStandalone;
            btnBrowseDuckStation.Enabled = isStandalone;
            txtDolphinPath.Enabled = isStandalone;
            btnBrowseDolphin.Enabled = isStandalone;
            txtCemuPath.Enabled = isStandalone;
            btnBrowseCemu.Enabled = isStandalone;

            // Update hint text if NOT standalone
            if (!isStandalone)
            {
                // [V30] Show the ACTUAL auto-resolved path per emulator instead of a generic hint.
                // The backend (EmulatorProfileAutomator) resolves these from the RetroBat registry key
                // (HKCU\Software\RetroBat\LatestKnownInstallPath -> <root>\emulators\<emu>). Displaying
                // them here makes the auto-detection visible and verifiable without checking Standalone,
                // which previously looked like "detection not working" (fields were disabled + generic hint).
                // FR: Afficher le chemin auto-résolu réel par émulateur au lieu d'un indice générique.
                // Le backend résout ces chemins depuis la clé registre RetroBat. Les afficher ici rend
                // la détection visible et vérifiable sans cocher Standalone, ce qui ressemblait avant
                // à un échec de détection (champs désactivés + indice générique).
                string retroBatPath = null;
                try { retroBatPath = RemapProfileManager.GetRetroBatPath(); } catch { }

                string emulatorsRoot = null;
                if (!string.IsNullOrEmpty(retroBatPath))
                {
                    string candidate = Path.Combine(retroBatPath, "emulators");
                    if (Directory.Exists(candidate)) emulatorsRoot = candidate;
                }

                txtPCSX2Path.Text = ResolveAutoEmulatorPath(emulatorsRoot, "pcsx2");
                txtDuckStationPath.Text = ResolveAutoEmulatorPath(emulatorsRoot, "duckstation");
                txtDolphinPath.Text = ResolveAutoEmulatorPath(emulatorsRoot, "dolphin-emu");
                txtCemuPath.Text = ResolveAutoEmulatorPath(emulatorsRoot, "cemu");

                txtPCSX2Path.ForeColor = Color.Gray;
                txtDuckStationPath.ForeColor = Color.Gray;
                txtDolphinPath.ForeColor = Color.Gray;
                txtCemuPath.ForeColor = Color.Gray;
            }
            else
            {
                // Restore actual paths from instance when re-enabled
                txtPCSX2Path.Text = Options.Instance.PCSX2Path;
                txtDuckStationPath.Text = Options.Instance.DuckStationPath;
                txtDolphinPath.Text = Options.Instance.DolphinPath;
                txtCemuPath.Text = Options.Instance.CemuPath;

                txtPCSX2Path.ForeColor = Color.White;
                txtDuckStationPath.ForeColor = Color.White;
                txtDolphinPath.ForeColor = Color.White;
                txtCemuPath.ForeColor = Color.White;
            }
        }

        // [V30] Resolves the auto-detected emulator folder for display in the disabled
        // non-standalone fields, mirroring EmulatorProfileAutomator.FindEmulatorSubDir logic.
        // (EN/FR: Résout le dossier émulateur auto-détecté pour affichage dans les champs
        // désactivés en mode non-Standalone, reflétant la logique de FindEmulatorSubDir.)
        private string ResolveAutoEmulatorPath(string emulatorsRoot, string emuFolder)
        {
            if (string.IsNullOrEmpty(emulatorsRoot))
                return "(Auto-detect: RetroBat not found in registry)";
            string path = Path.Combine(emulatorsRoot, emuFolder);
            return Directory.Exists(path) ? path + "  (auto)" : path + "  (auto - folder not found)";
        }

        /// <summary>
        /// EN: [V55z] Show/Hide the Custom rumble rows: visible only when the style
        /// combo is on "Custom" (3).
        /// FR: [V55z] Affiche/Masque les lignes du style personnalisé : visibles
        /// seulement si le style sélectionné est « Custom » (3).
        /// </summary>
        private void UpdateReloadRumbleCustomUi()
        {
            bool isCustom = cboReloadRumbleStyle != null && cboReloadRumbleStyle.SelectedIndex == 3;
            lblReloadRumbleTicks.Visible = isCustom;
            trkReloadRumbleTicks.Visible = isCustom;
            lblReloadRumbleTickOn.Visible = isCustom;
            nudReloadRumbleTickOnMs.Visible = isCustom;
            lblReloadRumbleTickOff.Visible = isCustom;
            nudReloadRumbleTickOffMs.Visible = isCustom;
        }

        /// <summary>
        /// EN: Checks active remap profiles for Shake mappings and locks/unlocks gesture settings accordingly.
        /// FR: Vérifie les profils remap actifs pour les mappings Shake et verrouille/déverrouille les paramètres de gestes.
        /// </summary>
        private void UpdateGestureLockState()
        {
            try
            {
                // EN: Check Mouse/Keyboard profile for P1 (FR: Vérifier le profil Souris/Clavier de P1)
                var mouseMappings = Options.Instance.P1Mappings;
                bool mouseShakeWiimoteMapped = mouseMappings != null && mouseMappings.AccelWiimoteShake != null &&
                    (mouseMappings.AccelWiimoteShake.Special != SpecialAction.None || mouseMappings.AccelWiimoteShake.Key != System.Windows.Forms.Keys.None);
                bool mouseShakeNunchukMapped = mouseMappings != null && mouseMappings.AccelNunchukShake != null &&
                    (mouseMappings.AccelNunchukShake.Special != SpecialAction.None || mouseMappings.AccelNunchukShake.Key != System.Windows.Forms.Keys.None);

                // EN: Check GamePad profile for P1 (FR: Vérifier le profil GamePad de P1)
                var padMappings = Options.Instance.P1GamePadMappings;
                bool padShakeWiimoteMapped = padMappings != null && padMappings.AccelWiimoteShake != null &&
                    padMappings.AccelWiimoteShake.TargetType != GamePadMotionTargetType.None;
                bool padShakeNunchukMapped = padMappings != null && padMappings.AccelNunchukShake != null &&
                    padMappings.AccelNunchukShake.TargetType != GamePadMotionTargetType.None;

                bool hasAnyShakeMapping = mouseShakeWiimoteMapped || mouseShakeNunchukMapped || padShakeWiimoteMapped || padShakeNunchukMapped;

                string lockReason = "";
                if (hasAnyShakeMapping)
                {
                    // EN: Build reason message (FR: Construire le message de raison)
                    if (mouseShakeWiimoteMapped || mouseShakeNunchukMapped)
                        lockReason += "Mouse/IR profile has Shake mapped. ";
                    if (padShakeWiimoteMapped || padShakeNunchukMapped)
                        lockReason += "GamePad profile has Shake mapped. ";
                    lockReason += "Remove the mapping to enable gestures.";
                }

                // EN: Lock/Unlock Shake Reload (FR: Verrouiller/Déverrouiller Shake Reload)
                optEnableShakeReload.Enabled = !hasAnyShakeMapping;
                if (hasAnyShakeMapping)
                {
                    optEnableShakeReload.Checked = false;
                    optEnableShakeReload.ForeColor = Color.Gray;
                    optEnableShakeReload.Text = "Shake Reload (Locked)";
                }
                else
                {
                    optEnableShakeReload.ForeColor = Color.White;
                    optEnableShakeReload.Text = "Shake Reload";
                }

                // EN: Lock/Unlock Grenade Gesture (FR: Verrouiller/Déverrouiller Grenade Gesture)
                optEnableGrenadeGesture.Enabled = !hasAnyShakeMapping;
                if (hasAnyShakeMapping)
                {
                    optEnableGrenadeGesture.Checked = false;
                    optEnableGrenadeGesture.ForeColor = Color.Gray;
                    optEnableGrenadeGesture.Text = "Grenade Gesture (Locked)";
                }
                else
                {
                    optEnableGrenadeGesture.ForeColor = Color.White;
                    optEnableGrenadeGesture.Text = "Grenade Gesture";
                }

                // EN: Show/Hide lock reason label (FR: Afficher/Masquer label de raison de verrouillage)
                if (lblGestureLockReason == null)
                {
                    lblGestureLockReason = new Label
                    {
                        Name = "lblGestureLockReason",
                        ForeColor = Color.FromArgb(255, 180, 0), // Orange warning color
                        Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                        AutoSize = false,
                        Size = new Size(340, 40),
                        Location = new Point(20, 310)
                    };
                    tabGestures.Controls.Add(lblGestureLockReason);
                }

                lblGestureLockReason.Text = hasAnyShakeMapping ? "⚠ " + lockReason : "";
                lblGestureLockReason.Visible = hasAnyShakeMapping;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning(string.Format("UpdateGestureLockState error: {0}", ex.Message));
            }
        }

        private Label lblGestureLockReason;

        // [V57d] EN: Propose installing/activating the UMDF2 host as soon as the mode is
        //     selected in the dropdown (user request) instead of waiting for Apply.
        //     On refuse/failure the selection reverts to the current mode; moving away
        //     from UMDF2 after an activation deactivates the devices right away.
        //     FR: Proposer l'installation/activation de l'hôte UMDF2 dès la sélection du
        //     mode dans la liste (demande utilisateur) au lieu d'attendre le Apply. En cas
        //     de refus/échec, la sélection revient au mode courant ; quitter UMDF2 après
        //     une activation stoppe les périphériques immédiatement.
        private void OptMouseMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_hmActivationFlowActive)
                return;

            string selected = optMouseMode.SelectedItem as string;
            if (selected == null)
                return;

            MouseMode selectedMode = MouseModeFromLabel(selected);

            if (selectedMode == MouseMode.RawInputUmdf)
            {
                // EN: Already the saved mode, or already activated this session -> nothing to propose
                // FR: Déjà le mode enregistré, ou déjà activé dans cette session -> rien à proposer
                if (Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf || _hmActivatedThisSession)
                    return;

                _hmActivationFlowActive = true;
                try
                {
                    if (ApplyMouseModeChange(MouseMode.RawInputUmdf))
                    {
                        _hmActivatedThisSession = true;
                        SimpleLogger.Instance.Info("[V57d] UMDF2 proposed and activated at selection time.");
                    }
                    else
                    {
                        // EN: Refused or failed - revert the dropdown to the current mode
                        // FR: Refusé ou échoué - ramener la liste au mode courant
                        optMouseMode.SelectedItem = MouseModeToLabel(Options.Instance.DefaultMouseMode);
                    }
                }
                finally
                {
                    _hmActivationFlowActive = false;
                }
            }
            else if (Options.Instance.DefaultMouseMode != MouseMode.RawInputUmdf && _hmActivatedThisSession)
            {
                // EN: The user moved away from UMDF2 after an activation this session:
                //     stop the devices right away so nothing lingers.
                // FR: L'utilisateur a quitté UMDF2 après une activation dans cette
                //     session : stopper les périphériques immédiatement.
                _hmActivatedThisSession = false;
                ServiceClient.HmDeactivate();
                SimpleLogger.Instance.Info("[V57d] UMDF2 deactivated (user moved away from the mode).");
            }
        }

        // [V57d] EN: Display labels for the input mode dropdown (General tab).
        //     FR: Libellés d'affichage pour le mode d'input (onglet Général).
        private static string MouseModeToLabel(MouseMode mode)
        {
            if (mode == MouseMode.SendInput) return "SendInput";
            if (mode == MouseMode.RawInputUmdf) return "RawInput (UMDF2)";
            return "RawInput (VMulti)";
        }

        private static MouseMode MouseModeFromLabel(string label)
        {
            if (label == "SendInput") return MouseMode.SendInput;
            if (label == "RawInput (UMDF2)") return MouseMode.RawInputUmdf;
            return MouseMode.RawInput; // EN/FR: unknown label falls back to VMulti RawInput
        }

        /// <summary>
        /// [V57d] EN: Handles the input mode transition with user validation for UMDF2.
        ///     Selecting "RawInput (UMDF2)" proposes installing the UMDF2 driver and its
        ///     locally-trusted self-signed certificate (through the service, WITHOUT any
        ///     UAC prompt or reboot). Leaving UMDF2 deactivates the virtual devices.
        ///     Returns true when the new mode can be saved.
        ///     FR: Gère la transition de mode d'input avec validation utilisateur pour
        ///     UMDF2. Choisir « RawInput (UMDF2) » propose d'installer le pilote UMDF2
        ///     et son certificat auto-signé trusté machine (via le service, SANS UAC ni
        ///     reboot). Quitter UMDF2 désactive les périphériques virtuels. Retourne
        ///     vrai si le nouveau mode peut être sauvegardé.
        /// </summary>
        private bool ApplyMouseModeChange(MouseMode newMode)
        {
            MouseMode current = Options.Instance.DefaultMouseMode;

            if (newMode == current)
                return true;

            // [V57d] EN: Already proposed and activated at selection time -> save without asking again
            //     FR: Déjà proposé et activé à la sélection -> sauvegarder sans redemander
            if (newMode == MouseMode.RawInputUmdf && _hmActivatedThisSession)
                return true;

            if (newMode == MouseMode.RawInputUmdf)
            {
                // [V57i] EN: HmHost (the UMDF2 virtual input host) is a FRAMEWORK-DEPENDENT
                //     .NET 10 x64 program - per user decision the runtime is a machine
                //     prerequisite, NOT a self-contained deployment. Check the machine
                //     BEFORE proposing the mode: without the runtime, HmHost can never
                //     start (it dies silently in session 0, no native Windows dialog). The
                //     prompt NEVER shows when the runtime is already installed.
                //     FR: HmHost (l'hôte d'entrée virtuelle UMDF2) est un programme .NET 10
                //     x64 FRAMEWORK-DEPENDENT - par décision utilisateur le runtime est un
                //     prérequis machine, PAS un déploiement self-contained. Vérifier la
                //     machine AVANT de proposer le mode : sans runtime, HmHost ne peut
                //     jamais démarrer (il meurt silencieusement en session 0, aucun dialogue
                //     natif Windows). Le prompt ne s'affiche JAMAIS si le runtime est déjà
                //     installé.
                if (!Core.DotNetRuntimeChecker.IsNet10RuntimeInstalled())
                {
                    string runtimePrompt =
                        "The RawInput (UMDF2) mode relies on HmHost, its virtual input host (a .NET 10 program).\n" +
                        "This machine does NOT have the .NET 10 runtime installed - HmHost cannot start.\n\n" +
                        "Download (free) from the official page:\n" +
                        "https://dotnet.microsoft.com/download/dotnet/10.0\n" +
                        "Column 'Run apps - Runtime' -> '.NET Runtime 10.0.x' -> button 'x64'\n" +
                        "(NOT the 'SDK', NOT 'ASP.NET Core Runtime', NOT '.NET Desktop Runtime')\n\n" +
                        "[Yes]    = Open the official download page\n" +
                        "[No]     = Continue anyway (activation will fail until the runtime is installed)\n" +
                        "[Cancel] = Abort\n\n" +
                        "--------------------------------\n\n" +
                        "Le mode RawInput (UMDF2) repose sur HmHost, son hôte d'entrée virtuelle (programme .NET 10).\n" +
                        "Cette machine n'a PAS le runtime .NET 10 installé - HmHost ne peut pas démarrer.\n\n" +
                        "Téléchargement (gratuit) depuis la page officielle :\n" +
                        "https://dotnet.microsoft.com/download/dotnet/10.0\n" +
                        "Colonne « Run apps - Runtime » -> « .NET Runtime 10.0.x » -> bouton « x64 »\n" +
                        "(PAS le « SDK », PAS « ASP.NET Core Runtime », PAS « .NET Desktop Runtime »)\n\n" +
                        "[Oui]    = Ouvrir la page de téléchargement officielle\n" +
                        "[Non]    = Continuer quand même (l'activation échouera tant que le runtime n'est pas installé)\n" +
                        "[Annuler] = Abandonner";

                    DialogResult rt = MessageBox.Show(this, runtimePrompt, ".NET 10 Runtime required / Runtime .NET 10 requis",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (rt == DialogResult.Yes)
                    {
                        // EN: Open the official .NET 10 download page, then abort the mode
                        //     selection - the user re-selects RawInput (UMDF2) after installing.
                        //     FR: Ouvre la page officielle de téléchargement .NET 10, puis
                        //     abandonne la sélection du mode - l'utilisateur re-sélectionnera
                        //     RawInput (UMDF2) après installation.
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "https://dotnet.microsoft.com/download/dotnet/10.0",
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            SimpleLogger.Instance.Error("[V57i] Failed to open the .NET download page: " + ex.Message);
                        }
                        return false;
                    }
                    if (rt == DialogResult.Cancel)
                        return false;
                    // EN: DialogResult.No = continue anyway -> fall through to the normal
                    //     activation flow (which will surface the diagnostic on failure).
                    //     FR: DialogResult.No = continuer quand même -> poursuivre le flux
                    //     d'activation normal (qui affichera le diagnostic en cas d'échec).
                    SimpleLogger.Instance.Warning("[V57i] UMDF2 selected without the .NET 10 runtime - continuing anyway at the user's request.");
                }

                string confirm =
                    "Enable the RawInput (UMDF2) input mode?\n\n" +
                    "This installs the UMDF2 virtual HID driver and its self-signed certificate\n" +
                    "(machine Root + TrustedPublisher stores) through the WiimoteGun Service.\n" +
                    "No UAC prompt, no test mode, no reboot. First activation can take a few seconds.\n" +
                    "Takes effect on the next Wiimote connection / app restart.\n\n" +
                    "Activer le mode d'input RawInput (UMDF2) ?\n\n" +
                    "Cela installe le pilote HID virtuel UMDF2 et son certificat auto-signé\n" +
                    "(magasins Root + TrustedPublisher de la machine) via le service WiimoteGun.\n" +
                    "Aucun UAC, aucun test mode, aucun reboot. La première activation peut prendre\n" +
                    "quelques secondes. Prend effet à la prochaine connexion de Wiimote / relance.";

                DialogResult choice = MessageBox.Show(this, confirm, "RawInput (UMDF2)",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (choice != DialogResult.Yes)
                    return false;

                this.Cursor = Cursors.WaitCursor;
                string resp = ServiceClient.HmActivate();
                this.Cursor = Cursors.Default;

                if (resp == null || !resp.StartsWith("OK"))
                {
                    MessageBox.Show(this,
                        "UMDF2 activation failed:\n" + (resp ?? "Service not running or too old (update the service).") +
                        "\n\nThe previous input mode is kept.\n\n" +
                        "Échec de l'activation UMDF2 - le mode d'input précédent est conservé.",
                        "RawInput (UMDF2)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                SimpleLogger.Instance.Info("[V57d] UMDF2 mode activated: " + resp);

                // [V57j] EN: Confirm the activation ONCE (instead of the silent
                //     WaitCursor-then-nothing flow) and let the user restart the app now -
                //     the mode takes effect on the next Wiimote connection / app restart,
                //     and a restart applies it immediately (the new instance re-activates
                //     HmHost silently and creates the devices per connected wiimote).
                //     FR: Confirmer l'activation UNE FOIS (au lieu du flux silencieux
                //     WaitCursor-puis-rien) et faire confirmer par l'utilisateur le
                //     redémarrage de l'app - le mode prend effet à la prochaine connexion
                //     de Wiimote / au redémarrage, et un redémarrage l'applique
                //     immédiatement (la nouvelle instance réactive HmHost silencieusement
                //     et crée les devices par wiimote connectée).
                string successMsg =
                    "RawInput (UMDF2) activated successfully.\n" +
                    "The mode takes effect on the next Wiimote connection or app restart.\n\n" +
                    "Restart Wiimote4Guns now?\n\n" +
                    "--------------------------------\n\n" +
                    "RawInput (UMDF2) activé avec succès.\n" +
                    "Le mode prend effet à la prochaine connexion de Wiimote ou au redémarrage de l'app.\n\n" +
                    "Redémarrer Wiimote4Guns maintenant ?";
                DialogResult restart = MessageBox.Show(this, successMsg, "RawInput (UMDF2)",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (restart == DialogResult.Yes)
                {
                    // EN: Save the freshly validated mode BEFORE restarting - neither the
                    //     selection-time flow nor BtnApply has persisted it yet, and the
                    //     restart batch reloads the options from XML (without this save
                    //     the restart would undo the user's selection).
                    //     FR: Sauvegarder le mode fraîchement validé AVANT de redémarrer -
                    //     ni le flux de sélection ni le BtnApply ne l'ont encore persisté,
                    //     et le batch de redémarrage recharge les options depuis le XML
                    //     (sans cette sauvegarde, le redémarrage annulerait la sélection
                    //     de l'utilisateur).
                    Options.Instance.DefaultMouseMode = MouseMode.RawInputUmdf;
                    Options.Instance.Save();
                    // EN: Reloads the options and relaunches via the restart batch (the
                    //     current instance exits cleanly; the new one applies the mode).
                    //     FR: Recharge les options et relance via le batch de redémarrage
                    //     (l'instance courante quitte proprement ; la nouvelle applique le
                    //     mode).
                    Program.RestartApplication();
                }
                return true;
            }

            // EN: Leaving UMDF2 - stop the virtual devices through the service
            // FR: Sortie d'UMDF2 - stopper les périphériques virtuels via le service
            if (current == MouseMode.RawInputUmdf)
            {
                ServiceClient.HmDeactivate();
                SimpleLogger.Instance.Info("[V57d] UMDF2 mode deactivated (switched to " + newMode + ").");
            }

            return true;
        }

        private void BtnApplyOptions_Click(object sender, EventArgs e)
        {
            try 
            {
                // General
                if (optMouseMode.SelectedItem != null)
                {
                    // [V57d] EN: Label<->enum mapping + UMDF2 activation flow with user validation
                    //     FR: Mapping libellé<->enum + flux d'activation UMDF2 avec validation utilisateur
                    MouseMode newMouseMode = MouseModeFromLabel(optMouseMode.SelectedItem.ToString());
                    if (ApplyMouseModeChange(newMouseMode))
                        Options.Instance.DefaultMouseMode = newMouseMode;
                    else
                        optMouseMode.SelectedItem = MouseModeToLabel(Options.Instance.DefaultMouseMode);
                }
                
                Options.Instance.MonitorId = (int)optMonitorId.Value;
                
                if (optLEDLayout.SelectedItem != null)
                    Options.Instance.LEDLayout = GetLEDLayoutFromName(optLEDLayout.SelectedItem.ToString());
                
                Options.Instance.IRSensitivity = (int)optIRSensitivity.Value;
                Options.Instance.ShowNotifications = optShowNotifications.Checked;
                Options.Instance.EnableGamePadSwapMode = optEnableGamePadSwap.Checked;
                Options.Instance.PersistentGamePads = optPersistentGamePads.Checked;
                Options.Instance.EnableFPSMode = optEnableFPSMode.Checked;
                if (optLockModeOnGameStart != null)
                    Options.Instance.LockModeOnGameStart = optLockModeOnGameStart.Checked;
                if (optAutoBtReset != null)
                    Options.Instance.AutoBtResetOnFail = optAutoBtReset.Checked;
                if (numBtResetDelay != null)
                    Options.Instance.AutoBtResetDelaySeconds = (int)numBtResetDelay.Value;

                // Save Log Level (EN/FR: Sauvegarder niveau de log)
                if (optLogLevel.SelectedItem != null)
                {
                    Options.Instance.LoggingLevel = (LogLevel)Enum.Parse(typeof(LogLevel), optLogLevel.SelectedItem.ToString());
                    SimpleLogger.Instance.Threshold = Options.Instance.LoggingLevel;
                }

                // Auto-Start
                Options.Instance.AutoStart = (AutoStartMode)optAutoStart.SelectedIndex;
                Options.Instance.ApplyAutoStart();



                // Detection
                Options.Instance.DetectDolphinbar = optDetectDolphin.Checked;
                Options.Instance.DetectBlueTooth = optDetectBluetooth.Checked;

                // Gestures (EN/FR: Gestes)
                Options.Instance.EnableOffScreenReload = optEnableOffScreenReload.Checked;
                Options.Instance.OffScreenReloadAuto = optOffScreenReloadAuto.Checked;
                Options.Instance.EnableShakeReload = optEnableShakeReload.Checked;

                // [V55y] Reload rumble (EN/FR: Vibration rechargement)
                Options.Instance.ReloadRumbleEnabled = chkReloadRumble.Checked;
                Options.Instance.ReloadRumbleIntensity = (int)trkReloadRumbleIntensity.Value;
                if (cboReloadRumbleStyle.SelectedIndex >= 0)
                    Options.Instance.ReloadRumbleStyle = cboReloadRumbleStyle.SelectedIndex;

                // [V55z] Custom style parameters (EN/FR: Paramètres du style personnalisé)
                Options.Instance.ReloadRumbleCustomTicks = (int)trkReloadRumbleTicks.Value;
                Options.Instance.ReloadRumbleCustomOnMs = (int)nudReloadRumbleTickOnMs.Value;
                Options.Instance.ReloadRumbleCustomOffMs = (int)nudReloadRumbleTickOffMs.Value;
                
                if (optShakeSensitivity.SelectedIndex != -1)
                    Options.Instance.ShakeSensitivity = optShakeSensitivity.SelectedIndex;
                
                if (optShakeDevice.SelectedIndex != -1)
                    Options.Instance.ShakeFromNunchuk = (optShakeDevice.SelectedIndex == 1);

                if (optGrenadeDevice.SelectedIndex != -1)
                    Options.Instance.GrenadeFromNunchuk = (optGrenadeDevice.SelectedIndex == 1);
                
                Options.Instance.EnableGrenadeGesture = optEnableGrenadeGesture.Checked;

                // Emulators
                Options.Instance.RestartOnDolphin = optRestartOnDolphin.Checked;
                Options.Instance.RestartOnCemu = optRestartOnCemu.Checked;

                // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
                Options.Instance.EnableIRSmoothing = optEnableIRSmoothing.Checked;
                Options.Instance.IRSmoothingStrength = (int)optIRSmoothingStrength.Value;
                Options.Instance.UseHighPerfTimers = optUseHighPerfTimers.Checked;
                Options.Instance.EnableHomographyCache = optEnableHomographyCache.Checked;
                Options.Instance.EnableDistanceCompensation = optEnableDistanceCompensation.Checked;
                Options.Instance.UseIRExtrapolation = optUseIRExtrapolation.Checked;
                Options.Instance.IRExtrapolationStrength = (float)optIRExtrapolationStrength.Value;
                Options.Instance.EnableVirtualPolling = optEnableVirtualPolling.Checked;
                Options.Instance.VirtualPollingRate = (int)optVirtualPollingRate.Value;
                Options.Instance.IRSmoothingStrengthV2 = (int)optIRSmoothingStrengthV2.Value;
                Options.Instance.IRExtrapolationStrengthV2 = (float)optIRExtrapolationStrengthV2.Value;
                Options.Instance.VirtualPollingRateV2 = (int)optVirtualPollingRateV2.Value;

                // Standalone - Only save paths if in standalone mode to avoid saving hints
                Options.Instance.StandaloneMode = optStandaloneMode.Checked;
                if (Options.Instance.StandaloneMode)
                {
                    Options.Instance.PCSX2Path = txtPCSX2Path.Text.Trim();
                    Options.Instance.DuckStationPath = txtDuckStationPath.Text.Trim();
                    Options.Instance.DolphinPath = txtDolphinPath.Text.Trim();
                    Options.Instance.CemuPath = txtCemuPath.Text.Trim();
                }

                // [V42] EmulationStation integration (EN/FR: Intégration EmulationStation)
                if (chkEsScripts != null) Options.Instance.EsScriptsEnabled = chkEsScripts.Checked;
                if (chkEsTileHotkey != null) Options.Instance.EsTileHotkeyEnabled = chkEsTileHotkey.Checked;
                if (numEsTileDelay != null) Options.Instance.EsTileHotkeyDelayMs = (int)(numEsTileDelay.Value * 1000);
                if (optLockModeOnGameStart != null) Options.Instance.LockModeOnGameStart = optLockModeOnGameStart.Checked;

                // Save
                Options.Instance.Save();

                // [V42] Apply scripts immediately (also re-verified at startup)
                // (EN/FR: Appliquer les scripts immédiatement (revérifié aussi au démarrage))
                try
                {
                    if (Options.Instance.EsScriptsEnabled)
                        EsScriptIntegration.EnsureScriptsInstalled();
                    else
                        EsScriptIntegration.RemoveScripts();
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning($"ES scripts management failed: {ex.Message}");
                }
                
                SimpleLogger.Instance.Info("Options saved. Restarting...");
                MessageBox.Show(this.FindForm(), "Options saved. Application will restart.", "Restart", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Restart Logic
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wiimotegun.exe",
                    Arguments = "-restart",
                    UseShellExecute = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                Process.Start(psi);
                Program.IsRestarting = true;
                Application.Exit();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error saving options: {ex.Message}");
                MessageBox.Show(this.FindForm(), $"Error saving options: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetLEDLayoutName(LEDLayoutType type)
        {
            switch (type)
            {
                case LEDLayoutType.Gun4IRDiamond: return "Gun4IR Diamond";
                case LEDLayoutType.TwoWiimoteBar: return "Two Wiimote Bars";
                case LEDLayoutType.FourCorners: return "Four Corners";
                default: return "Wiimote Bar";
            }
        }
        
        private LEDLayoutType GetLEDLayoutFromName(string name)
        {
            switch (name)
            {
                case "Gun4IR Diamond": return LEDLayoutType.Gun4IRDiamond;
                case "Two Wiimote Bars": return LEDLayoutType.TwoWiimoteBar;
                case "Four Corners": return LEDLayoutType.FourCorners;
                default: return LEDLayoutType.WiimoteBar;
            }
        }

        private void BtnConfigureGamePad_Click(object sender, EventArgs e)
        {
            using (Form form = new Form
            {
                Text = "GamePad Mapping Configuration",
                Size = new Size(580, 820),
                StartPosition = FormStartPosition.CenterParent,
                ShowIcon = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(20, 20, 20)
            })
            {

                var control = new GamePadMappingControl
                {
                    Dock = DockStyle.Fill
                };
                if (control.btnBack != null) control.btnBack.Visible = false;
                control.BackRequested += (s, args) => form.Close();
                
                form.Controls.Add(control);
                form.ShowDialog(this);
            }
        }


    }
}
