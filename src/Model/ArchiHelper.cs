// ArchiHelper.cs
// 
// Created: 2019-09-19T11:01 PM
// Updated: 2019-09-21T10:51 PM
// 
// Copyright 2019 (c) Jim Schilling
// All Rights Reserved.
// 
// MIT License

#region

using System;
using ArchiToolbox.Logging;

#endregion

namespace ArchiToolbox.Model
{
    public static class ArchiHelper
    {
        private static readonly Logger _logger = new Logger(nameof(ArchiHelper));

        public static string GetRootFolderName(string archiElementTypeName)
        {
            try
            {
                switch (archiElementTypeName)
                {
                    case "ApplicationComponent":
                    case "ApplicationCollaboration":
                    case "ApplicationInterface":
                    case "ApplicationFunction":
                    case "ApplicationInteraction":
                    case "ApplicationProcess":
                    case "ApplicationEvent":
                    case "ApplicationService":
                    case "DataObject":
                        return "Application";

                    case "Product":
                    case "BusinessActor":
                    case "BusinessRole":
                    case "BusinessCollaboration":
                    case "BusinessInterface":
                    case "BusinessProcess":
                    case "BusinessFunction":
                    case "BusinessInteraction":
                    case "BusinessEvent":
                    case "BusinessService":
                    case "BusinessObject":
                    case "Contract":
                    case "Representation":
                        return "Business";

                    case "Capability":
                    case "CourseOfAction":
                    case "Resource":
                        return "Strategy";

                    case "Node":
                    case "Device":
                    case "SystemSoftware":
                    case "TechnologyCollaboration":
                    case "Path":
                    case "CommunicationNetwork":
                    case "TechnologyFunction":
                    case "TechnologyProcess":
                    case "TechnologyInteraction":
                    case "TechnologyEvent":
                    case "TechnologyService":
                    case "Artifact":
                    case "Equipment":
                    case "Facility":
                    case "DistributionNetwork":
                    case "Material":
                        return "Technology & Physical";

                    case "Stakeholder":
                    case "Driver":
                    case "Assessment":
                    case "Goal":
                    case "Outcome":
                    case "Principle":
                    case "Requirement":
                    case "Constraint":
                    case "Meaning":
                    case "Value":
                        return "Motivation";

                    case "WorkPackage":
                    case "Deliverable":
                    case "ImplementationEvent":
                    case "Plateau":
                    case "Gap":
                        return "Implementation & Migration";

                    case "Location":
                    case "Grouping":
                    case "AndJunction":
                    case "OrJunction":
                        return "Other";


                    default:
                        throw new ArgumentException($"Unknown value for {nameof(archiElementTypeName)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(GetRootFolderName)}: {ex.Message}");
                throw;
            }
        }
    }
}