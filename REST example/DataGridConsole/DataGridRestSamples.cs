﻿using System;
using System.Collections.Generic;
using DataGridConsole.Filters;
using Newtonsoft.Json.Linq;

namespace DataGridConsole
{
    /// <summary>
    /// This class contains static methods for querying DataGrid via the DataGrid REST API.
    /// </summary>
    public static class DataGridRestSamples
    {
        #region Examples
        /// <summary>
        /// Prints the first 100 audit records at the instance level.
        /// </summary>
        /// <param name="client"></param>
        public static void PrintAuditRecords(RelativityHttpClient client)
        {
            // declare query parameters
            const int workspaceId = -1;
            const string requestUri = Constants.EndpointUris.GetAuditLogItems;
            const int itemsPerPage = 100;
            const int pageNumber = 1;
            const string sortBy = "TimeStamp";
            const string sortOrder = "desc";
            const bool includeDetails = false;
            const bool includeOldNewValues = false;

            string query = "*";
            // Note: if you specify a non-existent field in this list, then
            // you will receive 0 results.
            // These don't specify the fields returned in the query, but rather
            // the fields on which the query is operating on.
            List<string> fields = new List<string> { };
            string filterQuery = BuildFilterQuery(query, fields);


            // build out JSON Object as payload
            JObject payload = new JObject
            {
                ["workspaceId"] = workspaceId,
                ["request"] = new JObject
                {
                    {"itemsPerPage", itemsPerPage},
                    {"pageNumber", pageNumber},
                    {"sortBy", sortBy},
                    {"sortOrder", sortOrder},
                    {"includeDetails", includeDetails},
                    {"includeOldNewValues", includeOldNewValues},
                    {"filterQuery", filterQuery}
                }
            };

            try
            {
                JObject results = client.Post(requestUri, payload);
                Console.WriteLine(results.ToString());
                //JArray listOfResults = JArray.FromObject(results["Data"]);
                //Console.WriteLine($"Total: {listOfResults.Count}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }


        /// <summary>
        /// Prints out a JSON representation of the specified audit record ID.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="recordId"></param>
        public static void PrintSingleAuditRecord(RelativityHttpClient client, int recordId)
        {
            const string requestUri =
                Constants.EndpointUris.GetAuditLogItem;
            JObject payload = new JObject
            {
                ["workspaceId"] = -1, // use admin workspace
                ["request"] = new JObject
                {
                    {"Id", recordId}
                }
            };

            try
            {
                JObject results = client.Post(requestUri, payload);
                Console.WriteLine(results.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// Prints users who logged in in the last N days
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lastNumDays"></param>
        public static void PrintUsersLoggedInRecently(RelativityHttpClient client, int lastNumDays)
        {
            // declare query parameters
            const int workspaceId = -1;
            const string requestUri =
                Constants.EndpointUris.GetAuditLogItems;
            const int itemsPerPage = 1000;
            const int pageNumber = 1;
            const string sortBy = "TimeStamp";
            const string sortOrder = "desc";
            const bool includeDetails = false;
            const bool includeOldNewValues = false;

            string query = "*";
            List<string> fields = new List<string>();

            // filter for last N days
            TimeSpan timeSpan = new TimeSpan(lastNumDays, 0, 0, 0);
            TimestampFilter timeFilter = new TimestampFilter(Cmp.Gte, DateTime.Now.Subtract(timeSpan));

            // filter for Logins only
            ActionFilter actionFilter = new ActionFilter("Login");

            // now combine the two filters
            JObject combinedFilter = new JoinedFilter(timeFilter, actionFilter, BoolOp.And).GetFilter();
            string filterQuery = BuildFilterQuery(query, fields, combinedFilter);

            // build out JSON Object as payload
            JObject payload = new JObject
            {
                ["workspaceId"] = workspaceId,
                ["request"] = new JObject
                {
                    {"itemsPerPage", itemsPerPage},
                    {"pageNumber", pageNumber},
                    {"sortBy", sortBy},
                    {"sortOrder", sortOrder},
                    {"includeDetails", includeDetails},
                    {"includeOldNewValues", includeOldNewValues},
                    {"filterQuery", filterQuery}
                }
            };

            try
            {
                JObject results = client.Post(requestUri, payload);
                JArray listOfResults = JArray.FromObject(results["Data"]);

                // use a hash set to store unique users
                HashSet<string> users = new HashSet<string>();

                foreach (JToken result in listOfResults)
                {
                    JObject obj = (JObject) result;
                    string userName = (string) obj["UserName"];
                    // print out basic info
                    Console.WriteLine($"User: {userName}");
                    Console.WriteLine($"Timestamp: {obj["TimeStamp"]}");
                    users.Add(userName);
                    Console.WriteLine("--");
                }

                Console.WriteLine($"Total number of unique users: {users.Count}");
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine("One of the action names is invalid.");
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion


        #region Private helper methods

        /// <summary>
        /// Builds out a JSON object representing the filters/queries converted to string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="fields"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static string BuildFilterQuery(string query, IEnumerable<string> fields, JObject filter = null)
        {
            // JSON structure:
            // { 
            //   "filtered": { 
            //     "filter": {},
            //     "query": {
            //       "query_string": {
            //         "query": "<some query>",
            //         "fields": ["Field1", "Field2"]
            //       }
            //     }       
            //   }
            // }

            if (filter == null)
            {
                filter = new JObject();
            }

            JObject result = new JObject
            {
                ["filtered"] = new JObject
                {
                    ["filter"] = filter,
                    ["query"] = new JObject
                    {
                        ["query_string"] = new JObject
                        {
                            {"query", query},
                            {"fields", JArray.FromObject(fields)}
                        }
                    }
                }
            };

            return result.ToString();
        }

        #endregion
    }
}