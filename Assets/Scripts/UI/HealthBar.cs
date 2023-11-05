using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;

    public void SetHealth(int health)
    {
        slider.value = health;
        //set the current color to the corresponding one
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
        //set the default color to the end of the gradient
        fill.color = gradient.Evaluate(1f);
    }

    public void SetMana(float mana)
    {
        slider.value = mana;
        //set the current color to the corresponding one
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    public void SetMaxMana(float mana)
    {
        slider.maxValue = mana;
        slider.value = mana;
        //set the default color to the end of the gradient
        fill.color = gradient.Evaluate(1f);
    }

    public void DisableBar(bool activate)
    {
        gameObject.SetActive(activate);
    }
}
