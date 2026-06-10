using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using GsView = Autodesk.AutoCAD.GraphicsSystem.View;

namespace LegendBuilderWW.Services
{
    /// <summary>
    /// Renders AutoCAD geometry to a System.Drawing.Bitmap off-screen, using AutoCAD's own renderer
    /// (the "BlockView" GraphicsSystem technique) so hatches, linetypes, and colors look exactly as
    /// they will when generated. Used for the legend preview; later reused for per-row thumbnails.
    ///
    /// NOTE: the Autodesk.AutoCAD.GraphicsSystem API has shifted across releases — if this fails to
    /// compile against the installed AutoCAD 2023 SDK, the fixes are confined to this file. The whole
    /// render is wrapped so any runtime failure returns null and the caller shows "preview unavailable".
    /// </summary>
    public static class SymbolRenderer
    {
        /// <summary>
        /// Renders a block definition (by BlockTableRecord ObjectId) to a bitmap of the given size.
        /// Returns null if the block is empty or rendering fails.
        /// </summary>
        public static Bitmap RenderBlock(Document doc, ObjectId blockId, Size size, Color background)
        {
            if (doc == null || blockId.IsNull || size.Width <= 0 || size.Height <= 0) return null;

            Database db = doc.Database;
            Manager gsm = doc.GraphicsManager;
            Device device = null;
            GsView view = null;
            Model model = null;
            BlockReference reference = null;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Extents3d? extents = ComputeBlockExtents(tr, blockId);
                    if (!extents.HasValue) { tr.Commit(); return null; }

                    // AutoCAD 2015+ requires a graphics kernel for the off-screen device/model.
                    GraphicsKernel kernel = AcquireKernel();

                    device = gsm.CreateAutoCADOffScreenDevice(kernel);
                    device.OnSize(size);
                    device.DeviceRenderType = RendererType.Default;
                    device.BackgroundColor = background;

                    view = new GsView();
                    device.Add(view);
                    model = gsm.CreateAutoCADModel(kernel);

                    // A transient (non-database) reference to the block is what we draw.
                    reference = new BlockReference(Point3d.Origin, blockId);
                    view.Add(reference, model);

                    SetTopView(view, extents.Value, size);
                    view.Update();
                    device.Update();

                    Bitmap snapshot = view.GetSnapshot(new Rectangle(0, 0, size.Width, size.Height));
                    tr.Commit();
                    return snapshot;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (reference != null) { try { reference.Dispose(); } catch { } }
                if (view != null) { try { view.EraseAll(); } catch { } }
                if (device != null && view != null) { try { device.Erase(view); } catch { } }
                if (model != null) { try { model.Dispose(); } catch { } }
                if (view != null) { try { view.Dispose(); } catch { } }
                if (device != null) { try { device.Dispose(); } catch { } }
            }
        }

        private static GraphicsKernel AcquireKernel()
        {
            KernelDescriptor descriptor = new KernelDescriptor();
            descriptor.addRequirement(Autodesk.AutoCAD.UniqueString.Intern("3D Drawing"));
            return Manager.AcquireGraphicsKernel(descriptor);
        }

        private static Extents3d? ComputeBlockExtents(Transaction tr, ObjectId blockId)
        {
            BlockTableRecord btr = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
            if (btr == null) return null;

            Extents3d? ext = null;
            foreach (ObjectId id in btr)
            {
                Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;
                Extents3d? e = TryExtents(ent);
                if (!e.HasValue) continue;
                if (!ext.HasValue) { ext = e.Value; }
                else { Extents3d cur = ext.Value; cur.AddExtents(e.Value); ext = cur; }
            }
            return ext;
        }

        private static Extents3d? TryExtents(Entity ent)
        {
            try { return ent.GeometricExtents; }
            catch { return null; }
        }

        /// <summary>
        /// Aims a plan (top) view at the geometry, padded slightly and matched to the image aspect
        /// ratio so nothing is distorted or clipped.
        /// </summary>
        private static void SetTopView(GsView view, Extents3d ext, Size size)
        {
            Point3d min = ext.MinPoint;
            Point3d max = ext.MaxPoint;
            double cx = (min.X + max.X) / 2.0;
            double cy = (min.Y + max.Y) / 2.0;

            double w = max.X - min.X;
            double h = max.Y - min.Y;
            if (w < 1e-6) w = 1e-6;
            if (h < 1e-6) h = 1e-6;
            w *= 1.1;
            h *= 1.1;

            double imageAspect = (double)size.Width / size.Height;
            double extentAspect = w / h;
            if (extentAspect > imageAspect) h = w / imageAspect;
            else w = h * imageAspect;

            Point3d target = new Point3d(cx, cy, 0.0);
            Point3d position = new Point3d(cx, cy, 1.0);
            view.SetView(position, target, Vector3d.YAxis, w, h);
        }
    }
}
