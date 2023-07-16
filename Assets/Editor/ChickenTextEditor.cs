using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CanEditMultipleObjects]
[@CustomEditor(typeof(ChickenText), true)]
public class ChickenTextEditor : GraphicEditor
{
    SerializedProperty m_Text;
    SerializedProperty m_FontData;

    SerializedProperty m_OriginText;
    SerializedProperty m_EnableAreaMaskUV;

    bool panel_DisplayMatCache;

    List<string> keyList; 
    List<Material> valueList;

    protected override void OnEnable()
    {
        ChickenText sc = target as ChickenText;

        m_Text = serializedObject.FindProperty("m_Text");
        m_FontData = serializedObject.FindProperty("m_FontData");
        m_OriginText = serializedObject.FindProperty("origionText");
        m_EnableAreaMaskUV = serializedObject.FindProperty("areaMaskUV");

        panel_DisplayMatCache = false;

        keyList = new List<string>();
        valueList = new List<Material>();

        base.OnEnable();

        Undo.undoRedoPerformed += EditorCallback;
    }

    public void EditorCallback()
    {
        ChickenText sc = target as ChickenText;
        if (sc != null)
        {
            sc.SetAllDirty();
            sc.UpdateSubTextFontTex();
        }
    }

    public override void OnInspectorGUI()
    {
        ChickenText sc = target as ChickenText;

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_OriginText);
        EditorGUILayout.PropertyField(m_EnableAreaMaskUV);

        GUI.enabled = false;
        EditorGUILayout.PropertyField(m_Text); 
        GUI.enabled = true;

        EditorGUILayout.PropertyField(m_FontData);
        AppearanceControlsGUI();
        RaycastControlsGUI();
        MaskableControlsGUI();

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            sc.UpdateTextFromOriginText();
            sc.UpdateSubTextFontTex();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("ClearCacheData"))
        {
            sc.ClearAll();
        }

        panel_DisplayMatCache = EditorGUILayout.Foldout(panel_DisplayMatCache, "MatCache", true,
            new GUIStyle(EditorStyles.foldout){fontStyle = FontStyle.Bold});

        EditorGUI.indentLevel++;

        if (panel_DisplayMatCache)
        {
            keyList.Clear();
            valueList.Clear();

            sc.GetCacheMatData(ref keyList, ref valueList);

            GUI.enabled = false;

            GUILayout.BeginVertical();
            {
                for (int i = 0; i < keyList.Count; i++)
                {
                    EditorGUILayout.ObjectField(keyList[i], valueList[i], typeof(GameObject), true);
                }
            }
            GUILayout.EndVertical();

            GUI.enabled = true;
        }

        EditorGUI.indentLevel--;
    }
}



