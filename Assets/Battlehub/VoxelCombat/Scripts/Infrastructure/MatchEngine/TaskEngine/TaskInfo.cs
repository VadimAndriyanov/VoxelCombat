﻿using ProtoBuf;
using System.Runtime.Serialization;
using System.Collections;

namespace Battlehub.VoxelCombat
{
    public class ExpressionCode
    {
        public const int Value = 1;
        public const int Assign = 2;
        public const int Itertate = 3;
        public const int Get = 4;
        //public const int Set = 5;

        //Binary expressions
        public const int And = 10;
        public const int Or = 11;
        public const int Not = 12;

        //Comparation expression
        public const int Eq = 20;
        public const int NotEq = 21;
        public const int Lt = 22;
        public const int Lte = 23;
        public const int Gt = 24;
        public const int Gte = 25;

        //Arithmetic
        public const int Add = 30;
        public const int Sub = 31;

        //Complex expressions
        public const int UnitExists = 100;
        public const int UnitCoordinate = 101;
        public const int UnitState = 102;
        public const int UnitCanGrow = 103;
        public const int UnitCanSplit4 = 105;
 
        //Complex search expressions
        public const int EnemyVisible = 200;
        public const int FoodVisible = 201;

        //Task
        public const int TaskSucceded = 500;

    }

    public enum TaskType
    {
        Command = 1,
        Sequence = 2,
        Branch = 3,
        Repeat = 4,
        Break = 5,
        Continue = 6,
        Return = 7,
        Nop = 8,
        //Switch = 7,
        EvalExpression = 50,
        FindPath = 100,
        SearchForFood = 150,
        TEST_MockImmediate = 1000,
        TEST_Mock = 1001,
    }

    public enum TaskState
    {
        Idle,
        Active,
        Completed,
        //Failed,
        Terminated
    }

    [ProtoContract(AsReferenceDefault = true)]
    public class ExpressionInfo
    {
        [ProtoMember(1)]
        private int m_code;

        [ProtoMember(2, DynamicType = true)]
        private object m_value;

        [ProtoMember(3)]
        private ExpressionInfo[] m_children;

        public ExpressionInfo()
        {

        }

        public ExpressionInfo(int code, object value, params ExpressionInfo[] children)
        {
            m_code = code;
            m_value = value;
            m_children = children;
        }

        public int Code
        {
            get { return m_code; }
            set { m_code = value; }
        }

        public object Value
        {
            get { return m_value; }
            set { m_value = value; }
        }

        public ExpressionInfo[] Children
        {
            get { return m_children; }
            set { m_children = value; }
        }

        public bool IsEvaluating
        {
            get;
            set;
        }

        public static ExpressionInfo PrimitiveVar<T>(T val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Value,
                Value = PrimitiveContract.Create(val)
            };
        }

        public static ExpressionInfo Val(object val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Value,
                Value = val
            };
        }

        public static ExpressionInfo Assign(TaskInfo taskInfo, ExpressionInfo val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Assign,
                Value = taskInfo,
                Children = new[] { val, null }
            };
        }

        public static ExpressionInfo Iterate(IEnumerable enumerable)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Itertate,
                Value = enumerable.GetEnumerator()
            };
        }

        public static ExpressionInfo Assign(TaskInfo taskInfo, ExpressionInfo val, ExpressionInfo output)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Assign,
                Value = taskInfo,
                Children = new [] { val, output }
            };
        }

        public static ExpressionInfo Get(ExpressionInfo obj, ExpressionInfo propertyGetter)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Get,
                Children = new[] { obj, propertyGetter }
            };
        }

        public static ExpressionInfo Add(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Add,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Sub(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Sub,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo And(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.And,
                Children = new [] { left, right }
            };
        }

        public static ExpressionInfo Or(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Or,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Not(ExpressionInfo expressionInfo)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Not,
                Children = new[] { expressionInfo }
            };
        }

        public static ExpressionInfo Eq(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Eq,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo NotEq(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.NotEq,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Lt(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Lt,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Lte(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Lte,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Gt(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Gt,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Gte(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Gte,
                Children = new[] { left, right }
            };
        }


        public static ExpressionInfo UnitExists(ExpressionInfo unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitExists,
                Children = new[] { unitId, PrimitiveVar(playerId)}
            };
        }

        public static ExpressionInfo UnitState(ExpressionInfo unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitState,
                Children = new[] { unitId, PrimitiveVar(playerId) }
            };
        }

        public static ExpressionInfo UnitCoordinate(ExpressionInfo unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCoordinate,
                Children = new[] { unitId, PrimitiveVar(playerId) }
            };
        }

        public static ExpressionInfo UnitCanGrow(ExpressionInfo unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCanGrow,
                Children = new[] { unitId, PrimitiveVar(playerId) }
            };
        }

        public static ExpressionInfo TaskSucceded(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.TaskSucceded,
                Value = task,
            };
        }
    }

    [ProtoContract]
    public class TaskStateInfo
    {
        [ProtoMember(1)]
        public int TaskId;

        [ProtoMember(2)]
        public TaskState State;

        [ProtoMember(3)]
        public int PlayerId;

        [ProtoMember(4)]
        public int StatusCode;

        public bool IsFailed
        {
            get { return StatusCode != TaskInfo.TaskSucceded; }
        }

        public TaskStateInfo()
        {

        }

        public TaskStateInfo(int taskId, int playerId, TaskState state, int statusCode)
        {
            TaskId = taskId;
            PlayerId = playerId;
            State = state;
            StatusCode = statusCode;
        }
    }

    [ProtoContract(AsReferenceDefault = true)]
    public class TaskInputInfo
    {
        [ProtoMember(1)]
        public TaskInfo Scope;

        [ProtoMember(2)]
        public TaskInfo OutputTask;

        [ProtoMember(3)]
        public int OuputIndex;

        public void SetScope()
        {
            Scope = OutputTask.Parent;
        }
    }

    [ProtoContract(AsReferenceDefault = true)]
    public class TaskInfo
    {
        public const int TaskSucceded = 0;
        public const int TaskFailed = 1;

        [ProtoMember(1)]
        private int m_taskId;
        [ProtoMember(2)]
        private TaskType m_taskType;
        [ProtoMember(3)]
        private Cmd m_cmd;
        [ProtoMember(4)]
        private TaskState m_state;
        [ProtoMember(6)]
        private TaskInfo[] m_children;
        [ProtoMember(7)]
        private ExpressionInfo m_expression;
        [ProtoMember(8)]
        private bool m_requiresClientSidePreprocessing;
        [ProtoMember(9)]
        private TaskInputInfo[] m_inputs;
        [ProtoMember(10)]
        private int m_outputsCount;
        private int m_playerIndex = -1;

        public TaskInfo(TaskType taskType, Cmd cmd, TaskState state, ExpressionInfo expression, TaskInfo parent)
        {
            m_taskType = taskType;
            m_cmd = cmd;
            m_state = state;
            m_expression = expression;
            Parent = parent;
        }

        public TaskInfo(Cmd cmd, TaskState state, ExpressionInfo expression, TaskInfo parent)
            :this(TaskType.Command, cmd, state, expression, parent)
        {
        }

        public TaskInfo(Cmd cmd, TaskState state, ExpressionInfo expression)
            : this(TaskType.Command, cmd, state, expression, null)
        {
        }

        public TaskInfo(Cmd cmd, ExpressionInfo expression) 
            : this(TaskType.Command, cmd, TaskState.Idle, expression, null)
        {
        }

        public TaskInfo(Cmd cmd)
        : this(cmd, null)
        {
        }

        public TaskInfo(Cmd cmd, int playerIndex)
            : this(cmd, null)
        {
            m_playerIndex = playerIndex;
        }

        public TaskInfo(TaskType type)
            : this(type, new Cmd(CmdCode.Nop), TaskState.Idle, null, null)
        {
        }

        public TaskInfo(TaskType type, TaskState state)
           : this(type, new Cmd(CmdCode.Nop), state, null, null)
        {
        }

        public TaskInfo()
        {
        }

        public void Reset()
        {
            State = TaskState.Idle;
            StatusCode = TaskSucceded;
            PreprocessedCmd = null;
        }
    
        public int TaskId
        {
            get { return m_taskId; }
            set { m_taskId = value; }
        }

        public TaskType TaskType
        {
            get { return m_taskType; }
            set { m_taskType = value; }
        }

        public Cmd Cmd
        {
            get { return m_cmd; }
            set { m_cmd = value; }
        }

        public TaskState State
        {
            get { return m_state; }
            set { m_state = value; }
        }

        public TaskInfo Parent { get; set; }

        public TaskInfo[] Children
        {
            get { return m_children; }
            set { m_children = value; }
        }

        public ExpressionInfo Expression
        {
            get { return m_expression; }
            set { m_expression = value; }
        }

        public int PlayerIndex
        {
            get { return m_playerIndex; }
            set { m_playerIndex = value; }
        }

        public TaskInputInfo[] Inputs
        {
            get { return m_inputs; }
            set { m_inputs = value; }
        }

        public int OutputsCount
        {
            get { return m_outputsCount; }
            set { m_outputsCount = value; }
        }

        public bool RequiresClientSidePreprocessing
        {
            get { return m_requiresClientSidePreprocessing; }
            set { m_requiresClientSidePreprocessing = value; }
        }

        public Cmd PreprocessedCmd
        {
            get;
            set;
        }

        public int StatusCode
        {
            get;
            set;
        }

        public bool IsFailed
        {
            get { return StatusCode != TaskSucceded; }
        }

        [OnDeserialized]
        public void OnDeserializedMethod(StreamingContext context)
        {
            SetParents(this, false);
        }

        public void SetParents()
        {
            SetParents(this, true);
        }

        public void Initialize(int playerIndex = -1)
        {
            SetInputsScope(this, playerIndex, true);
        }


        private void SetInputsScope(TaskInfo taskInfo, int playerIndex, bool recursive)
        {
            if (taskInfo.Inputs != null)
            {
                for (int i = 0; i < taskInfo.Inputs.Length; ++i)
                {
                    taskInfo.Inputs[i].SetScope();
                }
            }

            taskInfo.PlayerIndex = playerIndex;
            if (taskInfo.Children != null)
            {
                for (int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    if (taskInfo.Children[i] != null)
                    {
                        if (recursive)
                        {
                            SetInputsScope(taskInfo.Children[i], playerIndex, recursive);
                        }
                    }
                }
            }
        }
        private static void SetParents(TaskInfo taskInfo, bool recursive)
        {
            if (taskInfo.Children != null)
            {
                for (int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    if(taskInfo.Children[i] != null)
                    {
                        taskInfo.Children[i].Parent = taskInfo;
                        if (recursive)
                        {
                            SetParents(taskInfo.Children[i], recursive);
                        }
                    }  
                }
            }
        }

        public static TaskInfo Repeat(ExpressionInfo expression, params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Repeat)
            {
                Expression = expression,
                Children = sequence,
            };
        }

        public static TaskInfo Sequence(params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Sequence)
            {
                Children = sequence,
            };
        }

        public static TaskInfo Branch(ExpressionInfo expression, TaskInfo yes, TaskInfo no = null)
        {
            return new TaskInfo(TaskType.Branch)
            {
                Expression = expression,
                Children = new[] { yes, no }
            };
        }

        public static TaskInfo Return(ExpressionInfo expression = null)
        {
            return new TaskInfo(TaskType.Return)
            {
                Expression = expression,
            };
        }


        public static TaskInfo EvalExpression(ExpressionInfo expression)
        {
            return new TaskInfo(TaskType.EvalExpression)
            {
                Expression = expression,
                OutputsCount = 1
            };
        }

        public static TaskInfo UnitOrAssetIndex(long unitOrAssetIndex)
        {
            return EvalExpression(ExpressionInfo.PrimitiveVar(unitOrAssetIndex));
        }

        public static TaskInfo FindPath(TaskInputInfo unitIndex)
        {
            TaskInfo findPath = new TaskInfo(TaskType.FindPath)
            {
                OutputsCount = 2
            };
            throw new System.NotImplementedException();
        }

        public static TaskInfo SearchForFood(TaskInputInfo unitIndex)
        {
            TaskInfo searchForFoodTask = new TaskInfo(TaskType.SearchForFood)
            {
                OutputsCount = 2
            };
            TaskInputInfo searchForFoodContext = new TaskInputInfo
            {
                OuputIndex = 0,
                OutputTask = searchForFoodTask,
            };

            searchForFoodTask.Inputs = new[] { searchForFoodContext, unitIndex };
            return searchForFoodTask;
        }
    }
}
