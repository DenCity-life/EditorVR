#if UNITY_EDITOR
using System;
using System.Collections;
#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class TooltipUI : MonoBehaviour, IWillRender
    {
        class AnimatedTooltipTextVisualData : IAnimateTransition
        {
            TMP_Text m_Text;
            string m_PreviousTextDisplayed;
            string m_TextToDisplay;
            bool m_PreviousTextFadedOut;
            float m_CurrentTextAlpha;
            float m_TargetTextAlpha;
            int m_StartingCharacterCount;
            int m_TargetCharacterCount;
            float m_CurrentIconSpacing;
            float m_TargetIconSpacing;
            LayoutElement m_OppositeSideSpacer;


            // Interface implementation
            public float currentTransitionAmount { get; private set; }
            public float targetTransitionAmount { get; private set; }
            public bool complete { get; private set; }

            public void Setup(TMP_Text text, LayoutElement oppositeSideSpacer)
            {
                m_Text = text;
                if (m_Text)
                {
                    //m_TextToDisplay = textToDisplay;
                    //m_TargetCharacterCount = m_TextToDisplay != null ? textToDisplay.Length : 0;
                    m_Text.alpha = 0f;
                }

                m_OppositeSideSpacer = oppositeSideSpacer;
                
                complete = true; // The update loop will be skipped on completed AnimatedTransitions
            }

            public void AssignNewContent(float iconSpacing, string textToDisplay = null)
            {
                var validText = !string.IsNullOrEmpty(textToDisplay);
                if (m_Text)
                {
                    m_PreviousTextDisplayed = m_Text.text;
                    m_StartingCharacterCount = m_PreviousTextDisplayed != null ? m_PreviousTextDisplayed.Length : 0;
                    m_TextToDisplay = textToDisplay;
                    m_TargetCharacterCount = validText ? textToDisplay.Length : 0;
                    m_CurrentTextAlpha = m_Text.alpha;
                    m_TargetTextAlpha = 0f; // Fade out any text that might be displaying
                    if (m_CurrentTextAlpha == 0)
                        m_Text.text = m_TextToDisplay;
                }

                m_CurrentIconSpacing = m_OppositeSideSpacer.minWidth;
                //m_TargetIconSpacing = forceHideSpacer && !validText ? 0 : m_TargetCharacterCount > 0 && !validText ? m_maxIconSpacing : k_IconTextMinSpacing;
                m_TargetIconSpacing = iconSpacing;
                currentTransitionAmount = 0f;
                complete = false;
                m_PreviousTextFadedOut = false;
            }

            public void Update(float deltaUpdate)
            {
                if (complete)
                    return;

                currentTransitionAmount += deltaUpdate;
                complete = currentTransitionAmount >= 1f;
                var smoothedAmount = Mathf.Clamp01(currentTransitionAmount);
                smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentTransitionAmount);

                var textFadeInOutAmount = Mathf.Repeat(Mathf.Sin(2 * smoothedAmount), 1f);
                if (!m_PreviousTextFadedOut && currentTransitionAmount > 0.5f) // Swap string at halfway point due to sin(2transitionAmount)
                {
                    m_PreviousTextFadedOut = true;
                    textFadeInOutAmount = 0f; // Hard-set zero for a single frame, allowing the text string can be swapped without being seen
                    m_TargetTextAlpha = 1f; // Set new fully opaque text goal value
                    m_Text.text = m_TextToDisplay; // Set the new text string before fading the alpha in
                    //m_StartingCharacterCount = m_TextToDisplay != null ? m_TargetCharacterCount : 0; // Being at zero if no text was displayed previously
                }
                m_Text.alpha = Mathf.Lerp(m_CurrentTextAlpha, m_TargetTextAlpha, textFadeInOutAmount);

                // Lerp via the unsmoothed value, for liner character reveal
                var maxVisibleCharacters = (int)Mathf.Lerp(m_StartingCharacterCount, m_TargetCharacterCount, currentTransitionAmount);
                m_Text.maxVisibleCharacters = maxVisibleCharacters;

                m_OppositeSideSpacer.minWidth = Mathf.Lerp(m_CurrentIconSpacing, m_TargetIconSpacing, smoothedAmount);

                // Also fade the icon in & out for icon transitions
                // Fade icon alpha out, then shrink it during the second 2sin phase
            }
        }

        class AnimatedTooltipIconVisualData : IAnimateTransition
        {
            readonly Color kTransparentColor = new Color(1f, 1f, 1f, 0f);

            // Icon
            Image m_Icon;
            Transform m_IconTransform;
            Sprite m_CurrentSprite;
            Sprite m_TargetSprite;
            Color m_CurrentColor;
            Color m_TargetColor;
            Color m_FinalColor;
            private Vector3 m_OriginalIconLocalScale;
            Vector3 m_CurrentIconLocalScale;
            Vector3 m_TargetIConLocalScale;
            bool m_PreviousIconFadedOut;

            // Interface implementation
            public float currentTransitionAmount { get; private set; }
            public float targetTransitionAmount { get; private set; }
            public bool complete { get; private set; }

            public void Setup(Image icon)
            {
                m_Icon = icon;
                m_IconTransform = m_Icon.gameObject.transform;
                m_OriginalIconLocalScale = m_Icon.gameObject.transform.localScale;
            }

            public void AssignNewContent(Sprite iconSprite = null)
            {
                m_CurrentIconLocalScale = m_IconTransform.localScale;
                m_Icon.sprite = iconSprite;
                m_TargetIConLocalScale = iconSprite == null ? Vector3.zero : m_OriginalIconLocalScale;
                //m_Icon.enabled = iconSprite != null; // TODO handle in update function
                m_CurrentSprite = m_Icon.sprite;
                m_TargetSprite = iconSprite;
                m_CurrentColor = m_Icon.color;
                m_TargetColor = kTransparentColor;

                currentTransitionAmount = 0f;
                m_PreviousIconFadedOut = false;
                complete = false;
            }

            public void Update(float deltaUpdate)
            {
                if (complete)
                    return;

                currentTransitionAmount += deltaUpdate;
                complete = currentTransitionAmount >= 1f;
                var smoothedAmount = Mathf.Clamp01(currentTransitionAmount);
                smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentTransitionAmount);

                var initialIconFadeOutAmount = Mathf.Repeat(Mathf.Sin(2 * smoothedAmount), 1f);
                if (!m_PreviousIconFadedOut && currentTransitionAmount > 0.5f) // Swap string at halfway point due to sin(2transitionAmount)
                {
                    m_PreviousIconFadedOut = true;
                    if (m_TargetSprite != null)
                    {
                        // Prepare new goal values in order to show the new sprite icon, after the previous was faded out
                        m_CurrentColor = kTransparentColor;
                        m_TargetColor = Color.white;
                        m_Icon.sprite = m_TargetSprite;
                    }
                }

                m_Icon.color = Color.Lerp(m_CurrentColor, m_TargetColor, initialIconFadeOutAmount);
                m_IconTransform.localScale = Vector3.Lerp(m_CurrentIconLocalScale, m_TargetIConLocalScale, smoothedAmount);
            }
        }

        class AnimatedTooltipBackgroundVisualData : IAnimateTransition
        {
            const float squareCornerBlendshapeTargetAmount = 90f;

            float m_CurrentBlendshapeWeight;
            float m_TargetBlendShapeWeight;
            SkinnedMeshRenderer m_BackgroundRenderer;
            SkinnedMeshRenderer m_BackgroundOutlineRenderer;

            public bool roundedCorners
            {
                set
                {
                    m_CurrentBlendshapeWeight = m_BackgroundRenderer.GetBlendShapeWeight(0);
                    m_TargetBlendShapeWeight = value ? 0f : squareCornerBlendshapeTargetAmount;
                    currentTransitionAmount = 0f;
                    complete = false;
                }
            }

            // Interface implementation
            public float currentTransitionAmount { get; private set; }
            public float targetTransitionAmount { get; private set; }
            public bool complete { get; private set; }

            public void Setup(SkinnedMeshRenderer backgroundRenderer, SkinnedMeshRenderer backgroundOutlineRenderer)
            {
                m_BackgroundRenderer = backgroundRenderer;
                m_BackgroundOutlineRenderer = backgroundOutlineRenderer;
            }

            public void Update(float deltaUpdate)
            {
                if (complete)
                    return;

                currentTransitionAmount += deltaUpdate;
                complete = currentTransitionAmount >= 1f;
                var smoothedAmount = Mathf.Clamp01(currentTransitionAmount);
                smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentTransitionAmount);
                var blendShapeWeight = Mathf.Lerp(m_CurrentBlendshapeWeight, m_TargetBlendShapeWeight, smoothedAmount);
                m_BackgroundRenderer.SetBlendShapeWeight(0, blendShapeWeight);
                m_BackgroundOutlineRenderer.SetBlendShapeWeight(0, blendShapeWeight);
            }
        }

        class AnimatedTooltipVisualDatax
        {
            const float kSquareCornerBlendshapeTargetAmount = 90f;
            readonly Color kTransparentColor = new Color(1f, 1f, 1f, 0f);

            // Icon
            Image m_Icon;
            Sprite m_CurrentSprite;
            Sprite m_TargetSprite;
            Color m_CurrentColor;
            Color m_TargetColor;
            Color m_FinalColor;
            Vector3 m_CurrentIconLocalScale;
            Vector3 m_TargetIConLocalScale;

            // Text
            TMP_Text m_Text;
            string m_TextToDisplay;
            bool m_PreviousTextFadedOut;
            float m_CurrentTextAlpha;
            float m_TargetTextAlpha;
            int m_StartingCharacterCount;
            int m_TargetCharacterCount;

            private float m_CurrentTransitionAmount;

            public void Setup(TMP_Text text, string textToDisplay = null, Image icon = null, Sprite iconSprite = null)
            {
                if (m_Text)
                    m_StartingCharacterCount = m_Text.text.Length;

                m_Text = text;
                if (m_Text)
                {
                    m_TextToDisplay = textToDisplay;
                    m_TargetCharacterCount = m_TextToDisplay != null ? textToDisplay.Length : 0;
                    m_CurrentTextAlpha = m_Text.alpha;
                    m_TargetTextAlpha = 0f; // Fade out any text that might be displaying
                }

                m_Icon = icon;
                if (m_Icon )
                {
                    m_CurrentIconLocalScale = m_Icon.gameObject.transform.localScale;
                    m_TargetIConLocalScale = Vector3.zero;
                    m_Icon.sprite = iconSprite;
                    //m_Icon.enabled = iconSprite != null; // TODO handle in update function
                    m_CurrentSprite = icon.sprite;
                    m_TargetSprite = iconSprite;
                    m_CurrentColor = m_Icon.color;
                    m_TargetColor = kTransparentColor;
                }

                m_CurrentTransitionAmount = 0f;
                m_PreviousTextFadedOut = false;
            }

            public void UpdateText(float deltaUpdate)
            {
                if (m_CurrentTransitionAmount > 1f)
                    return;

                var smoothedAmount = Mathf.Clamp01(m_CurrentTransitionAmount += deltaUpdate);
                smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(m_CurrentTransitionAmount);

                var textFadeInOutAmount = Mathf.Repeat(Mathf.Sin(2 * smoothedAmount), 1f);
                if (!m_PreviousTextFadedOut && m_CurrentTransitionAmount > 0.5f)
                {
                    m_PreviousTextFadedOut = true;
                    textFadeInOutAmount = 0f; // Hard-set zero for a single frame, allowing the text string can be swapped without being seen
                    m_TargetTextAlpha = 1f; // Set new fully opaque text goal value
                    m_Text.text = m_TextToDisplay; // Set the new text string before fading the alpha in
                    //m_StartingCharacterCount = m_TextToDisplay != null ? m_TargetCharacterCount : 0; // Being at zero if no text was displayed previously
                }
                m_Text.alpha = Mathf.Lerp(m_CurrentTextAlpha, m_TargetTextAlpha, textFadeInOutAmount);

                var maxCharacterCountAmount = (int)Mathf.Lerp(m_StartingCharacterCount, m_TargetCharacterCount, smoothedAmount);
                m_Text.maxVisibleCharacters = maxCharacterCountAmount;

                // Also fade the icon in & out for icon transitions
                // Fade icon alpha out, then shrink it during the second 2sin phase
            }

            public void UpdateIcon(float deltaUpdate)
            {
                if (m_CurrentTransitionAmount > 1f)
                    return;

                var smoothedAmount = Mathf.Clamp01(m_CurrentTransitionAmount += deltaUpdate);
                smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(m_CurrentTransitionAmount);

                var initialIconFadeOutAmount = Mathf.Repeat(Mathf.Sin(2 * smoothedAmount), 1f);
                if (!m_PreviousTextFadedOut && initialIconFadeOutAmount < 0)
                {
                    if (m_TargetSprite != null)
                    {
                        // Prepare new goal values in order to show the new sprite icon, after the previous was feded out
                        m_CurrentColor = kTransparentColor;
                        m_TargetColor = Color.white;
                    }
                }

                m_Icon.color = Color.Lerp(m_CurrentColor, m_TargetColor, smoothedAmount);
            }

            public void UpdateBorderShape(float deltaUpdate)
            {
                if (m_CurrentTransitionAmount > 1f)
                    return;
            }
        }

        const float k_IconTextMinSpacing = 4;
        const float k_IconTextSpacing = 14;

        enum TransitionState
        {
            Hidden,
            Transitioning,
            Visible
        };

        [SerializeField]
        RawImage m_DottedLine;

        [SerializeField]
        Transform[] m_Spheres;

        [SerializeField]
        Image m_Background;

        [SerializeField]
        Image m_Icon;
#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        TMP_Text m_TextLeft;

        [SerializeField]
        TMP_Text m_TextRight;
#endif
        [SerializeField]
        CanvasGroup m_LeftTextCanvasGroup;

        [SerializeField]
        CanvasGroup m_RightTextCanvasGroup;

        [SerializeField]
        LayoutElement m_LeftSpacer;

        [SerializeField]
        LayoutElement m_RightSpacer;

        [SerializeField]
        SkinnedMeshRenderer m_BackgroundRenderer;

        [SerializeField]
        SkinnedMeshRenderer m_BackgroundOutlineRenderer;

        // REMOVE DEMO FIELDS
        [Header("TESTING PARAMS")]
        [SerializeField]
        Sprite m_DEMOSPRITE;

        //TransitionState m_TransitionState = TransitionState.Hidden;
        bool m_Visible;
        int m_OriginalRightPaddingAmount;
        int m_OriginalTopPaddingAmount;
        int m_OriginalBottomPaddingAmount;
        int m_OriginalLeftPaddingAmount;
        float m_BackgroundUpdateScalar;

        TextAlignment m_Alignment;
        float m_maxIconSpacing;

        Coroutine m_VisibilityCoroutine;
        Coroutine m_AnimateShowLeftSideTextCoroutine;
        Coroutine m_AnimateShowRightSideTextCoroutine;

        readonly AnimatedTooltipTextVisualData m_RightTextAnimationContainer = new AnimatedTooltipTextVisualData();
        readonly AnimatedTooltipTextVisualData m_LeftTextAnimationContainer = new AnimatedTooltipTextVisualData();
        readonly AnimatedTooltipIconVisualData m_IconContainer = new AnimatedTooltipIconVisualData();
        readonly AnimatedTooltipBackgroundVisualData m_BackgroundContainer = new AnimatedTooltipBackgroundVisualData();

        public RawImage dottedLine { get { return m_DottedLine; } }
        public Transform[] spheres { get { return m_Spheres; } }
        public Image background { get { return m_Background; } }
        public RectTransform rectTransform { get { return m_Background.rectTransform; } }

        public bool transitioning
        {
            get
            {
                return !m_RightTextAnimationContainer.complete || !m_LeftTextAnimationContainer.complete;// || !m_IconContainer.complete;
            }
        }

        public event Action becameVisible;
        public Action<IWillRender> removeSelf { get; set; }

        void Awake()
        {
            m_RightTextAnimationContainer.Setup(m_TextRight, m_RightSpacer);
            m_LeftTextAnimationContainer.Setup(m_TextLeft, m_LeftSpacer);
            m_BackgroundContainer.Setup(m_BackgroundRenderer, m_BackgroundOutlineRenderer);
            m_IconContainer.Setup(m_Icon);
        }

        void Start()
        {
            //Show(m_DEMOTEXT, m_DEMOTEXTALIGNMENT);

            //m_Icon.enabled = false;
        }

        void Update()
        {
            var deltaScaled = Time.unscaledDeltaTime * 1f;
            m_RightTextAnimationContainer.Update(deltaScaled);
            m_LeftTextAnimationContainer.Update(deltaScaled);
            m_BackgroundContainer.Update(Time.unscaledDeltaTime * m_BackgroundUpdateScalar);
            m_IconContainer.Update(m_BackgroundUpdateScalar);
        }

        public void ShowX(string text, TextAlignment alignment, Sprite iconSprite = null)
        {
            // if Icon null, fade out opacity of current icon
            // if icon is not null, fade out current, fade in new icon
            var validText = !string.IsNullOrEmpty(text);
            var iconVisible = iconSprite != null;
            m_Icon.sprite = iconSprite;
            m_Icon.enabled = iconVisible;
            switch (alignment)
            {
                case TextAlignment.Center:
                case TextAlignment.Left:
                    // Treat center as left justified, aside from horizontal offset placement
#if INCLUDE_TEXT_MESH_PRO
                    m_TextRight.text = text;
                    m_TextRight.gameObject.SetActive(validText);
                    m_TextLeft.gameObject.SetActive(false);
                    //m_RightSpacer.minWidth = validText ? iconVisible ? m_IconTextSpacing : 0 : 0;
                    //m_LeftSpacer.minWidth = validText ? iconVisible ? k_IconTextMinSpacing : 8 : 0; ;
#endif
                    break;
                case TextAlignment.Right:
#if INCLUDE_TEXT_MESH_PRO
                    m_TextLeft.text = text;
                    m_TextRight.gameObject.SetActive(false);
                    m_TextLeft.gameObject.SetActive(validText);
                    //m_RightSpacer.minWidth = validText ? iconVisible ? k_IconTextMinSpacing : 8 : 0;
                    //m_LeftSpacer.minWidth = validText ? iconVisible ? m_IconTextSpacing : 0 : 0;
#endif
                    break;
            }

            if (!validText && iconVisible)
            {
                // Set rounded corners
                m_BackgroundRenderer.SetBlendShapeWeight(0, 0);
                m_BackgroundOutlineRenderer.SetBlendShapeWeight(0, 0);
            }
            else
            {
                // Set sharper/square corners
                m_BackgroundRenderer.SetBlendShapeWeight(0, 90);
                m_BackgroundOutlineRenderer.SetBlendShapeWeight(0, 90);
            }
        }

        public void AnimateShow(string text, TextAlignment alignment, Sprite iconSprite = null)
        {
            //text = Random.Range(0, 10) > 4f ? text : null;
            var validText = !string.IsNullOrEmpty(text);

            //m_TMPText.text = text;
            //this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateShowText());

            iconSprite = Random.Range(0, 10) > 4f ? m_DEMOSPRITE : (validText ? null : m_DEMOSPRITE); // TODO REMOVE
            var validIcon = iconSprite != null;

            //var animateLeftText = m_TextLeft.text.Length > 0;
            //var animateRightText = m_TextRight.text.Length > 0;

            // fade out the current text
            // adjust begin fading in the text alpa for new string, WHILE adding/removing characters

            m_BackgroundUpdateScalar = !validText ? 2f : 0.5f; // Faster return to the rounded corners on close of Tooltip
            m_BackgroundContainer.roundedCorners = !validText;
            var forceHideIfNoIconOrText = validIcon == false && validText == false;

            if (forceHideIfNoIconOrText)
            {
                m_RightTextAnimationContainer.AssignNewContent(0);
                m_LeftTextAnimationContainer.AssignNewContent(0);
            }
            else
            {
                switch (alignment)
                {
                    case TextAlignment.Center:
                    case TextAlignment.Left:
                        m_RightTextAnimationContainer.AssignNewContent(forceHideIfNoIconOrText ? 0 : validIcon ? k_IconTextMinSpacing : 10);
                        m_LeftTextAnimationContainer.AssignNewContent(validText && !validIcon ? 0 : !validIcon ? k_IconTextMinSpacing : k_IconTextSpacing, text);
                        break;
                    case TextAlignment.Right:
                        m_RightTextAnimationContainer.AssignNewContent(validText && !validIcon ? 0 : !validIcon ? k_IconTextMinSpacing : k_IconTextSpacing, text);
                        m_LeftTextAnimationContainer.AssignNewContent(forceHideIfNoIconOrText ? 0 : validIcon ? k_IconTextMinSpacing : 10);
                        break;
                }
            }


            m_IconContainer.AssignNewContent(iconSprite);

            //const float targetAmount = 1f;
            //var currentAmount = 0f;


            //m_VisibilityCoroutine = null;
        }

        IEnumerator AnimateShowText(TMP_Text text, CanvasGroup textCanvasGroup)
        {
            yield return null; // a frame is needed for proper UI param retrieval
            // set text
            // wait a frame for UI to adjust if needed
            // start anim with horiz layout group right padding inverse of m_originalHorizontalLAyoutPreferredWidth

            Vector3 targetDemoStartPosition = transform.localPosition;
            Vector3 currentDemoStartPosition = transform.localPosition;

            /*
            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = 3f;// isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var currentRightPaddingAmount = m_OriginalRightPaddingAmount;

            //var visibilityDefinition = definition.visibilityDefinition;
            //var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            //var shaderColorPropety = visibilityDefinition.colorProperty;
            textCanvasGroup.alpha = 0;

            RectOffset tempPadding = new RectOffset
            (
                m_OriginalLeftPaddingAmount,
                m_OriginalRightPaddingAmount,
                m_OriginalTopPaddingAmount,
                m_OriginalBottomPaddingAmount
            );

            m_TextContainerHorizontalLayoutGroup.padding = tempPadding;

            var textInfo = text.textInfo;
            var visibleCharacterCount = textInfo.characterCount;
            text.maxVisibleCharacters = visibleCharacterCount;
            yield return null;

            var startingAmount = -m_TextContainerHorizontalLayoutGroup.preferredWidth;
            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar), 4);
                //m_MiniBackgroundMask.localScale = Vector3.Lerp(m_OriginalMiniBackgroundLocalScale, m_OriginalMiniBackgroundLocalScale * 3, smoothedAmount);
                //smoothedAmount *= m_OriginalMinibackgroundOffsetValue;

                //transform.localPosition = Vector3.Lerp(targetDemoStartPosition, currentDemoStartPosition, smoothedAmount);

                var outlineExpandAmount = Mathf.Lerp(m_OriginalMinibackgroundOffsetValue, m_OriginalMinibackgroundOffsetValue * 30f, smoothedAmount);
                m_MiniBackgroundMask.offsetMin = new Vector2(-outlineExpandAmount, -outlineExpandAmount);
                m_MiniBackgroundMask.offsetMax = new Vector2(outlineExpandAmount, outlineExpandAmount);

                yield return null;
            }

            transform.localPosition = currentDemoStartPosition;

            currentAmount = 0f;
            textCanvasGroup.alpha = 1;
            speedScalar = 2f;
            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar), 2);
                smoothedAmount = Mathf.Lerp(smoothedAmount, currentAmount, smoothedAmount);
                tempPadding = new RectOffset(
                    m_OriginalLeftPaddingAmount,
                    (int)Mathf.Lerp(startingAmount, m_OriginalRightPaddingAmount, smoothedAmount),
                    m_OriginalTopPaddingAmount,
                    m_OriginalBottomPaddingAmount);

                smoothedAmount = Mathf.Pow(smoothedAmount, 4);
                m_TextContainerHorizontalLayoutGroup.padding = tempPadding;
                m_RightTextCanvasGroup.alpha = smoothedAmount;
                text.maxVisibleCharacters = (int)(smoothedAmount * visibleCharacterCount);

                yield return null;
            }

            // Wait before restarting
            currentAmount = 0f;
            while (currentAmount < kTargetAmount)
            {
                currentAmount += Time.unscaledDeltaTime;
                yield return null;
            }

            this.RestartCoroutine(ref m_AnimateShowTextCoroutine, AnimateHideText());
            */
        }

        public void OnBecameVisible()
        {
            if (becameVisible != null)
                becameVisible();
        }

        public void OnBecameInvisible()
        {
        }
    }
}
#endif
