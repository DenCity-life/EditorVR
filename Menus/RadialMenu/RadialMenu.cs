#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class RadialMenu : MonoBehaviour, IInstantiateUI, IAlternateMenu, IUsesMenuOrigins, ICustomActionMap,
        IControlHaptics, IUsesNode, IConnectInterfaces, IRequestFeedback
    {
        const float k_ActivationThreshold = 0.5f; // Do not consume thumbstick or activate menu if the control vector's magnitude is below this threshold

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        RadialMenuUI m_RadialMenuPrefab;

        [SerializeField]
        HapticPulse m_ReleasePulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;

        [SerializeField]
        HapticPulse m_ButtonClickedPulse;

        RadialMenuUI m_RadialMenuUI;
        List<ActionMenuData> m_MenuActions;
        Transform m_AlternateMenuOrigin;
        MenuHideFlags m_MenuHideFlags = MenuHideFlags.Hidden;

        readonly BindingDictionary m_Controls = new BindingDictionary();

        public event Action<Transform> itemWasSelected;

        public Transform rayOrigin { private get; set; }

        public Transform menuOrigin { get; set; }

        public GameObject menuContent { get { return m_RadialMenuUI.gameObject; } }

        public Node node { get; set; }

        public Bounds localBounds { get { return default(Bounds); } }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreLocking { get { return false; } }

        public List<ActionMenuData> menuActions
        {
            get { return m_MenuActions; }
            set
            {
                m_MenuActions = value;

                if (m_RadialMenuUI)
                    m_RadialMenuUI.actions = value;
            }
        }

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                m_AlternateMenuOrigin = value;

                if (m_RadialMenuUI != null)
                    m_RadialMenuUI.alternateMenuOrigin = value;
            }
        }

        public MenuHideFlags menuHideFlags
        {
            get { return m_MenuHideFlags; }
            set
            {
                if (m_MenuHideFlags != value)
                {
                    m_MenuHideFlags = value;
                    var visible = value == 0;
                    if (m_RadialMenuUI)
                        m_RadialMenuUI.visible = visible;

                    if (visible)
                        ShowFeedback();
                    else
                        this.ClearFeedbackRequests();
                }
            }
        }

        void Start()
        {
            m_RadialMenuUI = this.InstantiateUI(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenuUI>();
            m_RadialMenuUI.alternateMenuOrigin = alternateMenuOrigin;
            m_RadialMenuUI.actions = menuActions;
            this.ConnectInterfaces(m_RadialMenuUI); // Connect interfaces before performing setup on the UI
            m_RadialMenuUI.Setup();
            m_RadialMenuUI.buttonHovered += OnButtonHovered;
            m_RadialMenuUI.buttonClicked += OnButtonClicked;

            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var radialMenuInput = (RadialMenuInput)input;
            if (radialMenuInput == null || m_MenuHideFlags != 0)
            {
                this.ClearFeedbackRequests();
                return;
            }

            var inputDirection = radialMenuInput.navigate.vector2;

            if (inputDirection.magnitude > k_ActivationThreshold)
            {
                // Composite controls need to be consumed separately
                consumeControl(radialMenuInput.navigateX);
                consumeControl(radialMenuInput.navigateY);
                m_RadialMenuUI.buttonInputDirection = inputDirection;
            }
            else
            {
                m_RadialMenuUI.buttonInputDirection = Vector2.zero;
            }

            var selectControl = radialMenuInput.selectItem;
            m_RadialMenuUI.pressedDown = selectControl.wasJustPressed;
            if (m_RadialMenuUI.pressedDown)
                consumeControl(selectControl);

            if (selectControl.wasJustReleased)
            {
                this.Pulse(node, m_ReleasePulse);

                m_RadialMenuUI.SelectionOccurred();

                if (itemWasSelected != null)
                    itemWasSelected(rayOrigin);

                consumeControl(selectControl);
            }
        }

        void OnButtonClicked()
        {
            this.Pulse(node, m_ButtonClickedPulse);
        }

        void OnButtonHovered()
        {
            this.Pulse(node, m_ButtonHoverPulse);
        }

        void ShowFeedback()
        {
            List<VRInputDevice.VRControl> controls;
            if (m_Controls.TryGetValue("SelectItem", out controls))
            {
                foreach (var id in controls)
                {
                    this.AddFeedbackRequest(new ProxyFeedbackRequest
                    {
                        control = id,
                        node = node,
                        tooltipText = "Select Action (Press to Execute)"
                    });
                }
            }
        }
    }
}
#endif
