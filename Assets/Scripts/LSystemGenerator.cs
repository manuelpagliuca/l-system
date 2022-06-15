/* L-System Generator Project for the course
 * of Artificial Intelligence for Videogames.
 * Manuel Pagliuca, University of Milan, A.Y. 2021/2022 */

using System.Collections.Generic;
using UnityEngine;

public enum StartingTreeSet { First, Second, Third, Fourth };
public enum StartingRootSet { First, Second };
public struct TransformInfo
{
    public Vector3 position;
    public Quaternion rotation;
}

public struct RendererWidths
{
    public float start;
    public float end;
}

public struct State
{
    public TransformInfo transformInfo;
    public RendererWidths renderWidths;
    public float length;
}

public class LSystemGenerator : MonoBehaviour
{
    /// Tree predefined models
    private static Dictionary<int, string> treePredefinedModels = new Dictionary<int, string>
    {
        {0, "F[*X[FL]]F[*X[FL]]*X"},
        {1, "F[[X[F]]*X[F]]*F[*FX[FL]]*X"},
        {2, "F[*X][*X]" },
        {3, "F[*X[FL]][*X[FL]]" }
    };

    private static Dictionary<int, string> rootPredefinedModels = new Dictionary<int, string>
    {
        {0, "R[*X][*X]*R*[X]"},
        {1, "R[*X]*[*RX]*R*X"},
    };

    /// Inspector variables
    [SerializeField] public int seed = 1;
    [SerializeField] public string treeModel = treePredefinedModels[3];
    [SerializeField] public string rootModel = rootPredefinedModels[0];
    [SerializeField] public bool freeEditing = false;
    [SerializeField] public StartingTreeSet treePredefinedSets;
    [SerializeField] public StartingRootSet rootPredefinedSets;
    [Range(1, 6)] public int treeIterations = 3;
    [Range(0.1f, 1)] public float initialLength = 1f;
    [SerializeField] public GameObject branch;
    [SerializeField] public GameObject leaf;
    [SerializeField] public GameObject root;

    /// Private variables
    // Persistence of general purpose data about the tree
    private struct LSystemBufferedData
    {
        public string treeModel, rootModel;
        public int iterations;
        public int seed;
        public float initialLength;
        public GameObject branch, leaf;
    };
    LSystemBufferedData bufferedData;

    // State stack
    private Stack<State> stateStack = new Stack<State>();

    // Lists for branches and roots
    private List<GameObject> branches = new List<GameObject>();
    private List<GameObject> roots = new List<GameObject>();

    // Deriver & Parser
    private Derivator derivator;
    private Parser parser;

    // Rules
    private Dictionary<char, string> treeRules;
    private Dictionary<char, string> rootsRules;

    // Derivation strings
    private string treeDerivedStr = string.Empty;
    private string rootsDerivedStr = string.Empty;

    // Constant values
    private readonly char AXIOM = 'X';
    private readonly int MAX_ROOTS_ITERATIONS = 4;
    private readonly float TERRAIN_LOWER_BOUND = -1.0f;
    private readonly float SEGMENT_INITIAL_WIDTH = 0.1f;
    private readonly float SEGMENT_WIDTH_DECR = 0.03f;
    private readonly float SEGMENT_LENGTH_DECR = 0.01f;

    // Gameobject nodes for the branches and root lists
    private GameObject nodeBranches;
    private GameObject nodeRoots;


    /* Function executed before the Start(), it sets the deriver and
     * it created the main nodes for the branches and the roots. */
    private void Awake()
    {
        derivator = gameObject.GetComponent<Derivator>();
        parser = gameObject.GetComponent<Parser>();
        parser.SetLSGenerator(this);

        nodeBranches = new GameObject();
        nodeBranches.name = "Branches";
        nodeBranches.transform.parent = gameObject.transform;

        nodeRoots = new GameObject();
        nodeRoots.name = "Roots";
        nodeRoots.transform.parent = gameObject.transform;
    }

    /* Update function, if the free editing mode is disabled it keeps
     * using the selected predefined set, otherwise you can use the 
     * "freestyle" set (you can change the generative symbols in live and
     * check the results dynamically).*/
    private void Update()
    {
        if (CheckInspectorChanges())
        {
            DestroyTree();
            branches.Clear();
            roots.Clear();
            ResetOriginPosition();
            Start();
            Random.InitState(bufferedData.seed);
        }

        if (!freeEditing)
        {
            SelectPredefinedTree();
            SelectPredefinedRoot();
        }
    }

    private void Start()
    {
        BufferLSData();

        Random.InitState(seed);

        // Initialize the data structures
        treeRules = new Dictionary<char, string> { { AXIOM, bufferedData.treeModel } };
        rootsRules = new Dictionary<char, string> { { AXIOM, rootModel } };

        RendererWidths initWidths = new RendererWidths();
        initWidths.start = SEGMENT_INITIAL_WIDTH;
        initWidths.end = SEGMENT_INITIAL_WIDTH;

        State state = new State();
        state.length = bufferedData.initialLength - SEGMENT_LENGTH_DECR;
        state.renderWidths.start = SEGMENT_INITIAL_WIDTH;
        state.renderWidths.end = SEGMENT_INITIAL_WIDTH;

        stateStack.Push(state);

        GenerateBranches();
        ResetOriginPosition();

        GenerateRoots();
        ResetOriginPosition();

        AssignBranchesAndRootsNodes();
    }

    // It saves the data from the actual state for then next.
    private void BufferLSData()
    {
        bufferedData.treeModel = treeModel;
        bufferedData.rootModel = rootModel;
        bufferedData.iterations = treeIterations;
        bufferedData.initialLength = initialLength;
        bufferedData.branch = branch;
        bufferedData.leaf = leaf;
        bufferedData.seed = seed;
    }

    private void AssignBranchesAndRootsNodes()
    {
        foreach (GameObject branch in GameObject.FindGameObjectsWithTag("Branch"))
        {
            branch.transform.parent = nodeBranches.transform;
        }

        foreach (GameObject root in GameObject.FindGameObjectsWithTag("Root"))
        {
            root.transform.parent = nodeRoots.transform;
        }
    }

    public void DestroyTree()
    {
        GameObject[] leaves = GameObject.FindGameObjectsWithTag("Leaf");
        foreach (GameObject leaf in leaves)
        {
            Destroy(leaf);
        }

        GameObject[] branches = GameObject.FindGameObjectsWithTag("Branch");
        foreach (GameObject branch in branches)
        {
            Destroy(branch);
        }

        GameObject[] roots = GameObject.FindGameObjectsWithTag("Root");
        foreach (GameObject root in roots)
        {
            Destroy(root);
        }
    }

    /* Push the axiom in the tree string, derive the string given the axiom
     * and tree rules (using 'FF' for adding some distance from the base).
     * Then parse the string.*/
    private void GenerateBranches()
    {
        treeDerivedStr += AXIOM;
        derivator.SetAxiomAndRules(AXIOM, treeRules);
        treeDerivedStr = "FF" + derivator.Derive(treeIterations);
        parser.SetString(treeDerivedStr);
        parser.Parse();
    }

    /* Push the axiom in the tree string, derive the string given the axiom
     * and tree rules (using 'FF' for adding some distance from the base).
     * There is a limit on the number of iterations of 4 (maxTreeIterations)
     * that the roots can do respect to the branches.
     * Then parse the string.*/
    public void GenerateRoots()
    {
        rootsDerivedStr += AXIOM;
        derivator.SetAxiomAndRules(AXIOM, rootsRules);
        int rootsIterations = treeIterations > MAX_ROOTS_ITERATIONS ? MAX_ROOTS_ITERATIONS : treeIterations;
        rootsDerivedStr = derivator.Derive(rootsIterations);
        parser.SetString(rootsDerivedStr);
        parser.Parse();
    }

    public void Pop()
    {
        State predState = stateStack.Pop();
        transform.position = predState.transformInfo.position;
        transform.rotation = predState.transformInfo.rotation;
    }

    public void Push()
    {
        // push transform
        TransformInfo currentInfos = new TransformInfo();
        currentInfos.position = transform.position;
        currentInfos.rotation = transform.rotation;

        RendererWidths newWidth = new RendererWidths();
        newWidth.start = stateStack.Peek().renderWidths.end;
        newWidth.end = stateStack.Peek().renderWidths.start - SEGMENT_WIDTH_DECR;

        State state = new State();
        state.transformInfo = currentInfos;
        state.length = stateStack.Peek().length - SEGMENT_LENGTH_DECR;
        state.renderWidths = newWidth;

        stateStack.Push(state);
    }

    public void RotateRnd()
    {
        int toss = Random.Range(0, 4);
        float angle = Random.Range(10f, 35f);

        switch (toss)
        {
            case 0:
                transform.Rotate(Vector3.forward * angle);
                break;
            case 1:
                transform.Rotate(Vector3.back * angle);
                break;
            case 2:
                transform.Rotate(Vector3.left * angle);
                break;
            case 3:
                transform.Rotate(Vector3.right * angle);
                break;
        }
    }

    public void GenerateLeaf()
    {
        GameObject treeLeaf = Instantiate(leaf);
        treeLeaf.transform.position = transform.position;
        treeLeaf.transform.LookAt(transform);
        treeLeaf.transform.parent = (branches[branches.Count - 1]).transform;
    }

    /* Given the initial position it extract the root length, then it translate
     * down for that measure (will be saved in a variable 'toPosition').
     * 
     * Decrement the data directly in the buffered data (which is persistent).
     * 
     * Then check and respect (edit the values) the roots boundaries for the underground,
     * so that it avoids to have roots going too deep or coming out from the ground.
     * 
     * Istantiate the root object and set line renderer positions and width. */
    public void GenerateRoot()
    {
        // Get length and translate
        Vector3 initialPosition = transform.position;
        float rootLength = stateStack.Peek().length;
        transform.Translate(Vector3.down * rootLength);

        // Respect the boundaries
        Vector3 toPosition = transform.position;
        RespectRootBoundaries(ref initialPosition, ref toPosition);

        // Instantiate obj + set pos and width
        GameObject rootSegment = Instantiate(root);
        SetRootPositions(initialPosition, toPosition, rootSegment);
        SetSegmentWidth(rootSegment);

        roots.Add(rootSegment);
    }

    private void SetSegmentWidth(GameObject segment)
    {
        RendererWidths lastWidth = stateStack.Peek().renderWidths;
        segment.GetComponent<LineRenderer>().startWidth = lastWidth.start;
        segment.GetComponent<LineRenderer>().endWidth = lastWidth.end;
    }

    private static void SetRootPositions(Vector3 initialPosition, Vector3 toPosition, GameObject rootSegment)
    {
        rootSegment.GetComponent<LineRenderer>().SetPosition(0, initialPosition);
        rootSegment.GetComponent<LineRenderer>().SetPosition(1, toPosition);
    }

    private void ResetOriginPosition()
    {
        transform.SetPositionAndRotation(GetOriginPosition(), GetInitialRotation());
    }

    private void RespectRootBoundaries(ref Vector3 initialPosition, ref Vector3 toPosition)
    {
        if (toPosition.y >= TERRAIN_LOWER_BOUND)
            toPosition.y = TERRAIN_LOWER_BOUND;

        if (initialPosition.y >= TERRAIN_LOWER_BOUND && initialPosition != GetOriginPosition())
            initialPosition.y = TERRAIN_LOWER_BOUND;
    }
    public void GenerateBranch()
    {
        Vector3 initialPosition = transform.position;
        float branchesLength = stateStack.Peek().length;
        transform.Translate(Vector3.up * branchesLength);
        GameObject treeSegment = Instantiate(branch);
        treeSegment.GetComponent<LineRenderer>().SetPosition(0, initialPosition);
        treeSegment.GetComponent<LineRenderer>().SetPosition(1, transform.position);
        SetSegmentWidth(treeSegment);
        branches.Add(treeSegment);
    }

    Vector3 GetOriginPosition()
    {
        return new Vector3(0.0f, 0.0f, 10.0f);
    }

    private bool CheckInspectorChanges()
    {
        return CheckChangesInModels() ||
                treeIterations != bufferedData.iterations ||
                initialLength != bufferedData.initialLength ||
                branch != bufferedData.branch ||
                leaf != bufferedData.leaf ||
                seed != bufferedData.seed;
    }

    private bool CheckChangesInModels()
    {
        if (treeModel != bufferedData.treeModel || rootModel != bufferedData.rootModel)
        {

            return true;
        }
        return false;
    }

    private void SelectPredefinedTree()
    {
        string choice = string.Empty;

        switch (treePredefinedSets)
        {
            case StartingTreeSet.First:

                treeModel = treePredefinedModels[0];
                break;

            case StartingTreeSet.Second:
                treeModel = treePredefinedModels[1];
                break;

            case StartingTreeSet.Third:
                treeModel = treePredefinedModels[2];
                break;

            case StartingTreeSet.Fourth:
                treeModel = treePredefinedModels[3];
                break;
        }
    }

    private void SelectPredefinedRoot()
    {
        switch (rootPredefinedSets)
        {
            case StartingRootSet.First:
                rootModel = rootPredefinedModels[0];
                break;

            case StartingRootSet.Second:
                rootModel = rootPredefinedModels[1];
                break;
        }
    }


    Quaternion GetInitialRotation()
    {
        Quaternion originalRotation = new Quaternion();
        originalRotation.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
        return originalRotation;
    }
}
