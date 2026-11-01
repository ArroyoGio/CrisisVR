using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR
{
    // Colores de la guía v1.0: gris = inactivo, azul = activo, celeste = el rayo lo apunta.
    // Un objeto inactivo deshabilita su interactable, así que seleccionarlo no hace nada.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class SelectableHighlight : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public string id;
        [SerializeField] Renderer target;
        [SerializeField] Color inactiveColor = new Color(.45f, .47f, .5f);
        [SerializeField] Color activeColor = new Color(.13f, .38f, .78f);
        [SerializeField] Color hoverColor = new Color(.42f, .76f, .95f);

        XRSimpleInteractable interactable;
        MaterialPropertyBlock block;
        bool active;
        bool hovered;

        public bool Active => active;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (target == null) target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            interactable.hoverEntered.AddListener(_ => { hovered = true; Apply(); });
            interactable.hoverExited.AddListener(_ => { hovered = false; Apply(); });
            SetActive(active);
        }

        public void SetActive(bool value)
        {
            active = value;
            if (!value) hovered = false;
            if (interactable != null) interactable.enabled = value;
            Apply();
        }

        void Apply()
        {
            if (target == null || block == null) return;
            var color = !active ? inactiveColor : hovered ? hoverColor : activeColor;
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }
    }
}
