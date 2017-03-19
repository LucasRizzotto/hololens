﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

public class GeoMaker
{
  public enum PlatformType
  {
    Raised,
    Floating
  }

  public enum State
  {
    Idle,
    Select,
    AnimatedExtrude
  }

  public State state
  {
    get { return m_state; }
  }

  private State m_state = State.Idle;
  private List<GameObject> m_gameObjects = new List<GameObject>();
  private GameObject m_gameObject = null;
  private MeshCollider m_meshCollider;
  private Mesh m_mesh;
  private MeshRenderer m_meshRenderer;
  private PlatformType m_platformType;
  private PlanarTileSelection m_selection;
  private MeshExtruder m_meshExtruder;
  private Material m_extrudeMaterial;
  private float m_extrudeLength = 0;
  private float m_extrudeTime = 0;
  private float m_extrudeStart = 0;
  private Vector2[] m_simpleTopUV;
  private Vector2[] m_simpleSideUV;
  private Vector2[] m_cappedTopUV;
  private Vector2[] m_cappedCrownUV;
  private Vector2[] m_cappedBaseUV;

  private void CreateNewObject(Material material)
  {
    // Create reticle game object and mesh
    m_gameObject = new GameObject("Extruded-" + m_gameObjects.Count);
    m_gameObject.transform.parent = null;
    m_meshCollider = m_gameObject.AddComponent<MeshCollider>();
    m_mesh = m_gameObject.AddComponent<MeshFilter>().mesh;
    m_meshRenderer = m_gameObject.AddComponent<MeshRenderer>();
    m_meshRenderer.material = material;
    m_meshRenderer.enabled = true;
    m_gameObjects.Add(m_gameObject);
  }

  public void StartSelection(PlatformType type, Material material)
  {
    if (m_state != State.Idle)
      return;
    m_platformType = type;
    CreateNewObject(material);
    m_selection.Reset();
    m_state = State.Select;
    Debug.Log("Start selection");
  }

  public void FinishSelection(Material material, System.Action OnFinished)
  {
    m_state = State.AnimatedExtrude;
    m_meshExtruder = new MeshExtruder(m_selection);
    m_extrudeMaterial = material;
    m_extrudeLength = 0.3f;
    m_extrudeTime = 2;
    m_extrudeStart = Time.time;
    Debug.Log("Finish selection");
  }

  public void Update(Vector3 rayOrigin, Vector3 rayDirection, float rayLength)
  {
    if (m_state == State.Select)
    {
      m_selection.Raycast(rayOrigin, rayDirection, rayLength);
      Vector3[] vertices;
      int[] triangles;
      Vector2[] uv;
      m_selection.GenerateMeshData(out vertices, out triangles, out uv);
      if (vertices.Length > 0)
      {
        m_mesh.Clear();
        m_mesh.vertices = vertices;
        m_mesh.uv = uv;
        m_mesh.triangles = triangles;
        m_mesh.RecalculateBounds();
        //TODO: make a GetTransform function
        m_gameObject.transform.rotation = m_selection.rotation;
        m_gameObject.transform.position = m_selection.position;
        m_gameObject.transform.localScale = m_selection.scale;
      }
    }
    else if (m_state == State.AnimatedExtrude)
    {
      float delta = Time.time - m_extrudeStart;
      float extrudeLength = Mathf.Lerp(0, m_extrudeLength, delta / m_extrudeTime);
      Vector3[] vertices;
      int[] triangles;
      Vector2[] uv;
      //m_meshExtruder.ExtrudeSimple(out vertices, out triangles, out uv, extrudeLength, m_simpleTopUV, m_simpleSideUV, extrudeLength * 100);
      //m_meshExtruder.ExtrudeCapped(out vertices, out triangles, out uv, extrudeLength, m_cappedTopUV, m_cappedCrownUV, m_cappedBaseUV);
      m_meshExtruder.ExtrudeSimpleWithBottom(out vertices, out triangles, out uv, extrudeLength, m_simpleTopUV, m_simpleSideUV, extrudeLength * 100);
      m_mesh.Clear();
      m_mesh.vertices = vertices;
      m_mesh.uv = uv;
      m_mesh.triangles = triangles;
      m_mesh.RecalculateBounds();
      m_mesh.RecalculateNormals();
      m_meshRenderer.material = m_extrudeMaterial;
      m_meshCollider.sharedMesh = null;
      m_meshCollider.sharedMesh = m_mesh;
      if (delta >= m_extrudeTime)
      {
        switch (m_platformType)
        {
          case PlatformType.Raised:
            break;
          case PlatformType.Floating:
            m_gameObject.AddComponent<FloatingPlatform>();
            break;
        }
        m_state = State.Idle;
        m_gameObject = null;
        //TODO: call OnFinished()
      }
    }
  }

  public GeoMaker()
  {
    // Selection surface
    Vector2[] selectUV =
    {
      /*
      new Vector2((1f/128) * 0.5f, (1f/736) * (736 - 0.5f)),
      new Vector2((1f/128) * (128 - 0.5f), (1f/736) * (736 - 0.5f)),
      new Vector2((1f/128) * (128 - 0.5f), (1f/736) * 0.5f),
      new Vector2((1f/128) * 0.5f, (1f/736) * 0.5f)
      */
      (1f / 128) * new Vector2(3.5f, 128 - 3.5f),
      (1f / 128) * new Vector2(128 - 3.5f, 128 - 3.5f),
      (1f / 128) * new Vector2(128 - 3.5f, 3.5f),
      (1f / 128) * new Vector2(3.5f, 3.5f)
      
    };
    m_simpleTopUV = new Vector2[]
    {
      new Vector2((1f/512) * 0.5f, (1f/1024) * (1024 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1024) * (1024 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1024) * (512 - 0.5f)),
      new Vector2((1f/512) * 0.5f, (1f/1024) * (512 - 0.5f))
    };
    m_simpleSideUV = new Vector2[]
    {
      new Vector2((1f/512) * 0.5f, (1f/1024) * (512 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1024) * (512 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1024) * 0.5f),
      new Vector2((1f/512) * 0.5f, (1f/1024) * 0.5f)
    };
    m_cappedTopUV = new Vector2[]
    {
      new Vector2((1f/512) * 0.5f, (1f/1536) * (1536 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1536) * (1536 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1536) * (1024 - 0.5f)),
      new Vector2((1f/512) * 0.5f, (1f/1536) * (1024 - 0.5f))
    };
    m_cappedCrownUV = new Vector2[]
    {
      new Vector2((1f/512) * 0.5f, (1f/1536) * (1024 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1536) * (1024 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1536) * (512 - 0.5f)),
      new Vector2((1f/512) * 0.5f, (1f/1536) * (512 - 0.5f))
    };
    m_cappedBaseUV = new Vector2[]
    {
      new Vector2((1f/512) * 0.5f, (1f/1536) * (512 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1536) * (512 - 0.5f)),
      new Vector2((1f/512) * (512 - 0.5f), (1f/1536) * 0.5f),
      new Vector2((1f/512) * 0.5f, (1f/1536) * 0.5f)
    };
    m_selection = new PlanarTileSelection(70, selectUV);
  }
}
