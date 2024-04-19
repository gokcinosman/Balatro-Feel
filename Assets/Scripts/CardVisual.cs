using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.EventSystems;
using Unity.Collections;
using UnityEngine.UI;

public class CardVisual : MonoBehaviour
{
    private bool initalize = false;

    [Header("Card")]
    public Card parentCard;
    private Transform cardTransform;
    private Vector3 rotationDelta;
    private int savedIndex;
    Vector3 movementDelta;

    [Header("References")]
    public Transform visualShadow;
    private Vector2 shadowDistance;
    [SerializeField]  private Transform shakeParent;
    [SerializeField] private Transform tiltParent;

    [Header("Follow Parameters")]
    public float followSpeed = 30;

    [Header("Rotation Parameters")]
    public float rotationAmount = 20;
    public float rotationSpeed = 20;
    public float autoTiltAmount = 30;
    public float manualTiltAmount = 20;
    public float tiltSpeed = 20;

    [Header("Scale Parameters")]
    public float scaleOnHover = 1.15f;
    public float scaleOnSelect = 1.25f;
    public float scaleTransition = .15f;
    public Ease scaleEase = Ease.OutBack;

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;

    private float curveYOffset;
    private float curveRotationOffset;

    private void Start()
    {
        shadowDistance = visualShadow.localPosition;
    }

    public void Initialize(Card target, int index=0)
    {
        //Declarations
        parentCard = target;
        cardTransform = target.transform;

        //Event Listening
        parentCard.SelectEvent.AddListener(Select);
        parentCard.DeselectEvent.AddListener(Deselect);
        parentCard.PointerEnterEvent.AddListener(PointerEnter);
        parentCard.PointerExitEvent.AddListener(PointerExit);

        //Initialization
        initalize = true;

    }

    public void UpdateIndex(int length)
    {
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize) return;

        curveYOffset = curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence;
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());

        //Smooth Follow
        Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, 30 * Time.deltaTime);

        //Smooth Rotate
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta,movement,25*Time.deltaTime);
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x,-60,60));

        //Tilt Logic
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float tiltX = parentCard.isHovering ? ((offset.y*-1) * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (-curve.rotationInfluence * parentCard.SiblingAmount()));

        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine* autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine* autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z,tiltZ, tiltSpeed/2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);

    }

    public float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    private void Select(Card card)
    {
        GetComponent<Canvas>().overrideSorting = true;
        transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition += (-Vector3.up * 20);
        visualShadow.GetComponent<Canvas>().overrideSorting = false;
    }

    private void Deselect(Card card)
    {
        GetComponent<Canvas>().overrideSorting = false;
        transform.DOScale(1, scaleTransition*2).SetEase(scaleEase);

        visualShadow.localPosition = shadowDistance;
        visualShadow.GetComponent<Canvas>().overrideSorting = true;

    }
    private void PointerEnter(Card card)
    {
        transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * 5, .15f, 20, 1).SetId(2);
    }
    private void PointerExit(Card card)
    {
        if(EventSystem.current.currentSelectedGameObject != parentCard.gameObject)
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }
}
