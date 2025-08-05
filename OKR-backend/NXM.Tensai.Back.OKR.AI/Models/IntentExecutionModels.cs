using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Represents the result of multiple function executions
    /// </summary>
    public class IntentExecutionResult
    {
        /// <summary>
        /// Whether all intent executions were successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The combined message from all intent executions
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// List of individual function execution results
        /// </summary>
        public List<FunctionResultItem> Results { get; set; } = new List<FunctionResultItem>();
    }
    
    /// <summary>
    /// Represents a single function result item
    /// </summary>
    public class FunctionResultItem
    {
        /// <summary>
        /// The intent that was executed
        /// </summary>
        public string Intent { get; set; }
        
        /// <summary>
        /// The data returned by the function
        /// </summary>
        public object Data { get; set; }
        
        /// <summary>
        /// The type of entity that was affected
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// The ID of the entity that was affected
        /// </summary>
        public string EntityId { get; set; }
        
        /// <summary>
        /// The operation that was performed
        /// </summary>
        public string Operation { get; set; }
        /// <summary>
        /// The message to display to the user
        /// </summary>
        public string Message { get; set; }
    }
    
    /// <summary>
    /// Represents the result of a function execution
    /// </summary>
    public class FunctionExecutionResult
    {
        /// <summary>
        /// Whether the function execution was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The result object returned by the function
        /// </summary>
        public object Result { get; set; }
        
        /// <summary>
        /// The type of entity that was affected
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// The ID of the entity that was affected
        /// </summary>
        public string EntityId { get; set; }
        
        /// <summary>
        /// The operation that was performed
        /// </summary>
        public string Operation { get; set; }
        
        /// <summary>
        /// The message to display to the user
        /// </summary>
        public string Message { get; set; }
    }
}
