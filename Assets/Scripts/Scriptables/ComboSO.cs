using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable/Combo")]
public class ComboSO : ScriptableObject
{
    [SerializeField] List<AttackSO> combo;
}
