using System;
using UnityEngine;

namespace ChemLab.Chemistry
{
    /// <summary>
    /// Компонент реакции: реагент или продукт. ratio — относительные доли:
    /// 1 "единица" реакции превращает ratio мл каждого реагента в ratio мл каждого продукта.
    /// </summary>
    [Serializable]
    public class ReactionComponent
    {
        [Tooltip("id вещества")]
        public string substance;

        [Tooltip("Относительное количество (мл на единицу реакции)")]
        public float ratio = 1f;

        [Tooltip("Продукт выпадает в осадок (не смешивается с раствором)")]
        public bool precipitate;

        [Tooltip("Продукт — газ (улетает, показываются пузырьки)")]
        public bool gas;
    }

    /// <summary>
    /// Декларативное описание химической реакции. Загружается из JSON
    /// (Assets/Chemistry/Data/Reactions/*.json).
    /// </summary>
    [Serializable]
    public class ReactionData
    {
        public string id;
        public string name;

        [Tooltip("Объяснение "почему это происходит" — показывается игроку")]
        public string explanation;

        public ReactionComponent[] reactants;
        public ReactionComponent[] products;

        [Tooltip("Тепловой эффект: °C на каждую преобразованную единицу (мл первого реагента). Плюс — экзотермическая, минус — эндотермическая.")]
        public float heatPerUnitMl;

        [Tooltip("Предупреждение по технике безопасности")]
        public string hazardNote = "";

        [Tooltip("Реакцию можно проводить только в вытяжном шкафу (фаза вытяжного шкафа)")]
        public bool requiresFumeHood;
    }

    /// <summary>Корневой формат JSON-файла реакций.</summary>
    [Serializable]
    public class ReactionFile
    {
        public ReactionData[] reactions;
    }
}
