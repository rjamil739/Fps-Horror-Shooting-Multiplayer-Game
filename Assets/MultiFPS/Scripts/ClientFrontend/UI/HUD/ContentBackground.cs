using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MultiFPS.UI
{
    public class ContentBackground : MonoBehaviour
    {
        public RectTransform _rectTransform;
        [SerializeField] float _marging = 5f;

        RectTransform _myRectTransform;

        private void Awake()
        {
            _myRectTransform = GetComponent<RectTransform>();
        }

        public void OnSizeChanged()
        {
            StopAllCoroutines();
            StartCoroutine(SetRect());

            IEnumerator SetRect()
            {
                yield return new WaitForEndOfFrame();
                _myRectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x + _marging * 2f, _rectTransform.sizeDelta.y + _marging * 2f);
                _myRectTransform.position = _rectTransform.position;
            }
        }
    }
}