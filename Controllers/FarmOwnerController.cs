using AgroClassLib.FarmOwner;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace GSTAgroTourism.Controllers
{
    public class FarmOwnerController : Controller
    {

        public async Task<ActionResult> ActivityAG()
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            string FarmHouseCode = "FH001"; // temporary (later from session/login)

            DataSet ds = await objbalfarm.GetActivitiesAG(FarmHouseCode);

            List<FarmActivitiesAG> list = new List<FarmActivitiesAG>();

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    list.Add(new FarmActivitiesAG
                    {
                        ActivityId = Convert.ToInt32(dr["ActivityId"]),
                        ActivityCode = dr["ActivityCode"].ToString(),
                        FarmHouseCode = dr["FarmHouseCode"].ToString(),
                        ActivityName = dr["ActivityName"].ToString(),
                        Duration = dr["Duration"].ToString(),
                        Price = Convert.ToDecimal(dr["Price"]),

                        StartDate = dr["StartDate"] == DBNull.Value? (DateTime?)null: Convert.ToDateTime(dr["StartDate"]),

                        EndDate = dr["EndDate"] == DBNull.Value? (DateTime?)null: Convert.ToDateTime(dr["EndDate"]),

                        Description = dr["Description"].ToString(),
                        ImagePath = dr["ImagePath"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public async Task<JsonResult> InsertActivityAG(FarmActivitiesAG obj, HttpPostedFileBase ImageFile)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            obj.FarmHouseCode = "FH001";

            obj.ActivityCode = "AC" + DateTime.Now.Ticks.ToString().Substring(8);

            // default image path
            obj.ImagePath = "";

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                string rootPath = Server.MapPath("~/Content/Image/Farms/");
                string farmFolder = Path.Combine(rootPath, obj.FarmHouseCode);

                if (!Directory.Exists(farmFolder))
                {
                    Directory.CreateDirectory(farmFolder);
                }

                string fileName = obj.ActivityCode + "_" + Path.GetFileName(ImageFile.FileName);
                string fullPath = Path.Combine(farmFolder, fileName);

                ImageFile.SaveAs(fullPath);

                obj.ImagePath = "/Content/Image/Farms/" + obj.FarmHouseCode + "/" + fileName;
            }

            await objbalfarm.InsertActivityAG(obj);

            return Json(true);
        }

        public async Task<JsonResult> GetActivityByIdAG(int id)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            var activity = await objbalfarm.GetActivityByIdAG(id);

            return Json(activity, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> UpdateActivityAG(FarmActivitiesAG obj, HttpPostedFileBase ImageFile)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            // Temporary hardcoded farm
            obj.FarmHouseCode = "FH001";

            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                // File type validation
                string ext = Path.GetExtension(ImageFile.FileName).ToLower();

                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    return Json(false);
                }

                string rootPath = Server.MapPath("~/Content/Image/Farms/");
                string farmFolder = Path.Combine(rootPath, obj.FarmHouseCode);

                if (!Directory.Exists(farmFolder))
                {
                    Directory.CreateDirectory(farmFolder);
                }

                string fileName = Guid.NewGuid().ToString() +
                        Path.GetExtension(ImageFile.FileName);

                string fullPath = Path.Combine(farmFolder, fileName);

                ImageFile.SaveAs(fullPath);

                obj.ImagePath = "/Content/Image/Farms/" +
                                obj.FarmHouseCode + "/" + fileName;
            }

            await objbalfarm.UpdateActivityAG(obj);

            return Json(true);
        }

        [HttpPost]
        public async Task<JsonResult> DeleteActivityAG(int id)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();

            await objbalfarm.DeleteActivityAG(id);

            return Json(true);
        }
        [HttpPost]
        public async Task<JsonResult> ToggleActivityAG(int id, bool isActive)
        {
            BALFarmOwner objbalfarm = new BALFarmOwner();
            await objbalfarm.ToggleActivityAG(id, isActive);
            return Json(true);
        }
    }
}