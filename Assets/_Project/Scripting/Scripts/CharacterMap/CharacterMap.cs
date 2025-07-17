using UnityEngine;

[CreateAssetMenu(fileName = "CharacterMap", menuName = "Game/CharacterMap")]
public class CharacterMap : ScriptableObject
{
    [Header("Character Settings")]
    [Space(5)]
    public CharacterMapType characterMapType = CharacterMapType.none;
    public string id;
    public int price = 100;

    [Header("Visual Assets")]
    [Space(5)]
    public Material material;
    public Mesh mesh;
    public Vector2 materialOffset = Vector2.zero;
}

public enum CharacterMapType
{
    none,
    hair,
    eyes,
    cheeks,
    mouth,
    torso,
    legs,
    feet,
    skin
}