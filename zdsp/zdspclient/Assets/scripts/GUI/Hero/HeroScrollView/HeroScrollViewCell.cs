﻿using FancyScrollView;
using Kopio.JsonContracts;
using UnityEngine;
using UnityEngine.UI;
using Zealot.Repository;

public class HeroScrollViewCell : FancyScrollViewCell<HeroCellDto, HeroScrollViewContext>
{
    [SerializeField]
    Animator animator;
    [SerializeField]
    Text message;
    [SerializeField]
    Image image;
    [SerializeField]
    Button button;
    [SerializeField]
    GameObject highlight;
    [SerializeField]
    Material grayScaleMat;

    static readonly int scrollTriggerHash = Animator.StringToHash("scroll");
    HeroScrollViewContext context;

    void Start()
    {
        var rectTransform = transform as RectTransform;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchoredPosition3D = Vector3.zero;
        UpdatePosition(0);

        button.onClick.AddListener(OnPressedCell);
    }

    /// <summary>
    /// コンテキストを設定します
    /// </summary>
    /// <param name="context"></param>
    public override void SetContext(HeroScrollViewContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// セルの内容を更新します
    /// </summary>
    /// <param name="itemData"></param>
    public override void UpdateContent(HeroCellDto itemData)
    {
        message.text = itemData.Message;

        if (context != null)
        {
            var isSelected = context.SelectedIndex == DataIndex;
            highlight.SetActive(isSelected);

            HeroJson heroJson = HeroRepo.GetHeroById(itemData.HeroId);
            if (heroJson != null)
            {
                Sprite sprite = ClientUtils.LoadIcon(heroJson.portraitpath);
                if (sprite != null)
                    image.sprite = sprite;
            }
            image.material = itemData.Unlocked ? null : grayScaleMat;
        }
    }

    /// <summary>
    /// セルの位置を更新します
    /// </summary>
    /// <param name="position"></param>
    public override void UpdatePosition(float position)
    {
        currentPosition = position;
        animator.Play(scrollTriggerHash, -1, position);
        animator.speed = 0;
    }

    // GameObject が非アクティブになると Animator がリセットされてしまうため
    // 現在位置を保持しておいて OnEnable のタイミングで現在位置を再設定します
    float currentPosition = 0;
    void OnEnable()
    {
        UpdatePosition(currentPosition);
    }

    void OnPressedCell()
    {
        if (context != null)
        {
            context.OnPressedCell(this);
        }
    }
}

