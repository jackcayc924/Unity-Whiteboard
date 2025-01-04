using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Visualization.Tools.Whiteboard
{
    [RequireComponent(typeof(Slider))]
    public class VRSlider : MonoBehaviour, IPointerClickHandler, IDragHandler
    {
        private Slider slider;

        void Awake()
        {
            slider = GetComponent<Slider>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UpdateSliderValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateSliderValue(eventData);
        }

        private void UpdateSliderValue(PointerEventData eventData)
        {
            // Assuming eventData.pointerCurrentRaycast contains the hit information
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                Vector3 hitPosition = eventData.pointerCurrentRaycast.worldPosition;

                // Calculate the slider value based on the hit position
                // This example assumes a horizontal slider. For a vertical slider, you would use the y-coordinate.
                Vector3 localHitPosition = slider.transform.InverseTransformPoint(hitPosition);
                float normalizedValue = Mathf.InverseLerp(-slider.GetComponent<RectTransform>().sizeDelta.x / 2, slider.GetComponent<RectTransform>().sizeDelta.x / 2, localHitPosition.x);

                slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, normalizedValue);
            }
        }
    }
}
