using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RadialSection
{
    public SpriteRenderer iconRenderer = null;
    public UnityEvent onPress = new UnityEvent();
}