using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsAppPrueba1
{
    public partial class Personas : Form
    {
        #region Variables       
        private Capture videoCapture = null;
        private static Image<Bgr, Byte> currentFrame = null;
        private Mat frame = new Mat();
        private bool facesDetectionEnabled = false;
        private readonly CascadeClassifier faceCasacdeClassifier = new CascadeClassifier(@"C:\Users\ltabares\source\repos\WpfAppPrueba\WindowsFormsAppPrueba1\haarcascade_frontalface_alt.xml");



        private List<Persona> lstPersonas = new List<Persona>();


        #endregion
        public Personas()
        {
            InitializeComponent();
            GetData();
        }

        private void btnInsertar_Click(object sender, EventArgs e)
        {
            byte[] data = ImageToByteArray(picDetected.Image);

            //var image = new Image<Bgr, Byte>(new Bitmap(picDetected.Image));
            SqlConnection con = new SqlConnection("Data Source=localhost;Initial Catalog=Prueba;Persist Security Info=True;User ID=sa;Password=linKmobiL1");
            con.Open();
            SqlCommand cmd = new SqlCommand("insert into Persona values (@ccPersona,@nombrePersona,@apellidoPersona,@fechaNacimiento,@foto)", con);
            cmd.Parameters.AddWithValue("@ccPersona", Convert.ToInt32(txtCedula.Text));
            cmd.Parameters.AddWithValue("@nombrePersona", txtNombres.Text);
            cmd.Parameters.AddWithValue("@apellidoPersona", txtApellidos.Text);
            cmd.Parameters.AddWithValue("@fechaNacimiento", Convert.ToDateTime(dtFechaNacimiento.Value));
            cmd.Parameters.AddWithValue("@foto", Convert.ToBase64String(data, 0, data.Length));

            cmd.ExecuteNonQuery();
            con.Close();

            Limpiar();

            MessageBox.Show("Se ingreso con exito el registro");
            
            GetData();
            Application.Idle += null;
        }

        private void GetData()
        {
            try
            {
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                DataTable table = new DataTable
                {
                    Locale = CultureInfo.InvariantCulture
                };
                SqlConnection con = new SqlConnection("Data Source=localhost;Initial Catalog=Prueba;Persist Security Info=True;User ID=sa;Password=linKmobiL1");
                con.Open();

                // Create a new data adapter based on the specified query.
                dataAdapter = new SqlDataAdapter("select * from Persona", con);

                // Create a command builder to generate SQL update, insert, and
                // delete commands based on selectCommand.
                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);

                // Populate a new data table and bind it to the BindingSource.

                dataAdapter.Fill(table);
                dataGridView.DataSource = table;

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    Persona newPersona = new Persona
                    {
                        IdPersona = Convert.ToInt32(table.Rows[i].ItemArray[0].ToString()),
                        CcPersona = Convert.ToInt32(table.Rows[i].ItemArray[1].ToString()),
                        NombrePersona = table.Rows[i].ItemArray[2].ToString(),
                        ApellidoPersona = table.Rows[i].ItemArray[3].ToString(),
                        FechaNacimiento = Convert.ToDateTime(table.Rows[i].ItemArray[4].ToString()),
                        Foto = table.Rows[i].ItemArray[5].ToString()
                        //Foto = Encoding.ASCII.GetBytes(table.Rows[i].ItemArray[5].ToString())
                    };
                    lstPersonas.Add(newPersona);
                }
                

                // Resize the DataGridView columns to fit the newly loaded content.
                dataGridView.AutoResizeColumns(
                    DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);

                dataAdapter.Dispose();
                con.Close();
            }
            catch (SqlException)
            {
                MessageBox.Show("To run this example, replace the value of the " +
                    "connectionString variable with a connection string that is " +
                    "valid for your system.");
            }
        }



        private void btnCargaFoto_Click(object sender, EventArgs e)
        {

            //Image<Bgr, Byte> resultImage = currentFrame.Convert<Bgr, Byte>();
            //picDetected.SizeMode = PictureBoxSizeMode.StretchImage;
            //picDetected.Image = resultImage.Bitmap;
            //string path = Directory.GetCurrentDirectory() + @"\ImagenesCapturadas";
            //if (!Directory.Exists(path))
            //    Directory.CreateDirectory(path);
            ////we will save 10 images with delay a second for each image 
            ////to avoid hang GUI we will create a new task
            //Task.Factory.StartNew(() =>
            //{
            //    for (int i = 0; i < 1; i++)
            //    {
            //        //resize the image then saving it
            //        resultImage.Resize(200, 200, Inter.Cubic).Save(path + @"\" + txtNombres.Text + "_" + DateTime.Now.ToString("dd-mm-yyyy-hh-mm-ss") + ".jpg");
            //        Thread.Sleep(2000);
            //    }
            //});
            //EnableSaveImage = false;
            //Application.Idle += null;


        }

        private void btnActivarCamara_Click(object sender, EventArgs e)
        {
            facesDetectionEnabled = true;
            if (videoCapture != null) videoCapture.Dispose();
            videoCapture = new Capture();
            Application.Idle += ProcessFrame;
        }



        private void Limpiar()
        {
            videoCapture.Dispose();
            videoCapture = null;
            currentFrame = null;
            frame = null;
            facesDetectionEnabled = false;
            picCapture.Image = null;
            picDetected.Image = null;
            
        }

        private byte[] ImageToByteArray(Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            //Step 1: Video Capture
            if (videoCapture != null && videoCapture.Ptr != IntPtr.Zero)
            {
                videoCapture.Retrieve(frame, 0);
                currentFrame = frame.ToImage<Bgr, Byte>().Resize(picCapture.Width, picCapture.Height, Inter.Cubic);

                //Step 2: Face Detection
                if (facesDetectionEnabled)
                {

                    //Convert from Bgr to Gray Image
                    Mat grayImage = new Mat();
                    CvInvoke.CvtColor(currentFrame, grayImage, ColorConversion.Bgr2Gray);
                    //Enhance the image to get better result
                    CvInvoke.EqualizeHist(grayImage, grayImage);

                    Rectangle[] faces = faceCasacdeClassifier.DetectMultiScale(grayImage, 1.1, 3,Size.Empty, Size.Empty);
                    //If faces detected
                    if (faces.Length > 0)
                    {

                        foreach (var face in faces)
                        {
                            //Draw square around each face 
                            CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.GreenYellow).MCvScalar, 2);

                            //Step 3: Add Person 
                            //Assign the face to the picture Box face picDetected
                            Image<Bgr, Byte> resultImage = currentFrame.Convert<Bgr, Byte>();
                            resultImage.ROI = face;
                            picDetected.SizeMode = PictureBoxSizeMode.StretchImage;
                            picDetected.Image = resultImage.Bitmap;

                            //if (EnableSaveImage)
                            //{
                            //    //We will create a directory if does not exists!
                            //    string path = Directory.GetCurrentDirectory() + @"\ImagenesCapturadas";
                            //    if (!Directory.Exists(path))
                            //        Directory.CreateDirectory(path);
                            //    //we will save 10 images with delay a second for each image 
                            //    //to avoid hang GUI we will create a new task
                            //    Task.Factory.StartNew(() =>
                            //    {
                            //        for (int i = 0; i < 1; i++)
                            //        {
                            //            //resize the image then saving it
                            //            resultImage.Resize(200, 200, Inter.Cubic).Save(path + @"\" + txtNombres.Text + "_" + DateTime.Now.ToString("dd-mm-yyyy-hh-mm-ss") + ".jpg");
                            //            Thread.Sleep(2000);
                            //        }
                                    
                            //    });
                            //    EnableSaveImage = false;
                                
                            //}


                            //if (btnAddPerson.InvokeRequired)
                            //{
                            //    btnAddPerson.Invoke(new ThreadStart(delegate {
                            //        btnAddPerson.Enabled = true;
                            //    }));
                            //}

                            // Step 5: Recognize the face 
                            //if (isTrained)
                            //{

                            //    Image<Gray, Byte> grayFaceResult = resultImage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                            //    CvInvoke.EqualizeHist(grayFaceResult, grayFaceResult);
                            //    var result = recognizer.Predict(grayFaceResult);

                            //    picCapture.Image = grayFaceResult.Bitmap;
                            //    pictureBox2.Image = TrainedFaces[result.Label].Bitmap;
                            //    Debug.WriteLine(result.Label + ". " + result.Distance);

                            //    //Here results found known faces
                            //    if (result.Label != -1 && result.Distance < 2000)
                            //    {
                            //        CvInvoke.PutText(currentFrame, PersonsNames[result.Label], new Point(face.X - 2, face.Y - 2),
                            //            FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                            //        CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Green).MCvScalar, 2);

                            //    }
                            //    ////here results did not found any know faces
                            //    //else
                            //    //{
                            //    //    CvInvoke.PutText(currentFrame, "Unknown", new Point(face.X - 2, face.Y - 2),
                            //    //        FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                            //    //    CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Red).MCvScalar, 2);

                            //    //}

                            //}
                        }
                    }
                }

                //Render the video capture into the Picture Box picCapture
                picCapture.Image = currentFrame.Bitmap;
            }

            //Dispose the Current Frame after processing it to reduce the memory consumption.
            if (currentFrame != null)
                currentFrame.Dispose();
        }

        private void ReconocimientoFacial(object sender, EventArgs e)
        {

            Mat frameReconocimiento = new Mat();
            Image<Bgr, Byte> currentFrameReconocimiento = null;
            int ImagesCount = 0;
            double Threshold = 2000;

            EigenFaceRecognizer recognizerReconocimiento = new EigenFaceRecognizer(ImagesCount, Threshold);
            List<Image<Gray, Byte>> TrainedFaces = new List<Image<Gray, Byte>>();
            List<int> PersonsLabes = new List<int>();
            List<string> PersonsNames = new List<string>();



            //Step 1: Video Capture
            if (videoCapture != null && videoCapture.Ptr != IntPtr.Zero)
            {
                videoCapture.Retrieve(frameReconocimiento, 0);
                currentFrameReconocimiento = frameReconocimiento.ToImage<Bgr, Byte>().Resize(picReconocimiento.Width, picReconocimiento.Height, Inter.Cubic);

                //Step 2: Face Detection
                //Convert from Bgr to Gray Image
               // Mat grayImage = new Mat();
                CvInvoke.CvtColor(currentFrameReconocimiento, frameReconocimiento, ColorConversion.Bgr2Gray);
                //Enhance the image to get better result
                CvInvoke.EqualizeHist(frameReconocimiento, frameReconocimiento);

                Rectangle[] faces = faceCasacdeClassifier.DetectMultiScale(frameReconocimiento, 1.1, 3, Size.Empty, Size.Empty);
                //Rectangle[] faces = faceCasacdeClassifier.DetectMultiScale(frameReconocimiento, 1.1, 3, new Size((frameReconocimiento.Size.Width / 3), (frameReconocimiento.Size.Height / 3)));
                //If faces detected
                if (faces.Length > 0)
                {
                    foreach (var face in faces)
                    {
                        //Draw square around each face 
                        CvInvoke.Rectangle(currentFrameReconocimiento, face, new Bgr(Color.GreenYellow).MCvScalar, 2);

                        //Step 3: Add Person 
                        //Assign the face to the picture Box face picDetected
                        Image<Bgr, Byte> resultImage = currentFrameReconocimiento.Convert<Bgr, Byte>();
                        resultImage.ROI = face;
                        picDetected.SizeMode = PictureBoxSizeMode.StretchImage;
                        picDetected.Image = resultImage.Bitmap;

                        //Step 4: Recorrer listado de personas con sus respectivas fotos
                        foreach (var item in lstPersonas)
                        {

                            Bitmap bmpReturn = null;

                            Byte[] byteBuffer = Convert.FromBase64String(item.Foto);
                            MemoryStream memoryStream = new MemoryStream(byteBuffer);
                            memoryStream.Position = 0;
                            bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);
                            memoryStream.Close();

                            Image<Gray, Byte> trainedImage = new Image<Gray, Byte>(bmpReturn).Resize(200, 200, Inter.Cubic);
                            CvInvoke.EqualizeHist(trainedImage, trainedImage);
                            TrainedFaces.Add(trainedImage);
                            PersonsLabes.Add(ImagesCount);
                            PersonsNames.Add(item.NombrePersona);
                            ImagesCount++;
                        }

                        if (TrainedFaces.Count() > 0)
                        {
                            recognizerReconocimiento = new EigenFaceRecognizer(ImagesCount, Threshold);
                            recognizerReconocimiento.Train<Gray, Byte>(TrainedFaces.ToArray(), PersonsLabes.ToArray());
                        }

                        Image<Gray, Byte> grayFaceResult = resultImage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                        CvInvoke.EqualizeHist(grayFaceResult, grayFaceResult);
                        var result = recognizerReconocimiento.Predict(grayFaceResult);

                        picCapture.Image = grayFaceResult.Bitmap;
                        //pictureBox2.Image = TrainedFaces[result.Label].Bitmap;
                        Debug.WriteLine(result.Label + ". " + result.Distance);

                        if (result.Label != -1 && result.Distance > 1)
                        {
                            CvInvoke.PutText(currentFrameReconocimiento, PersonsNames[2], new Point(face.X - 2, face.Y - 2),
                                FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                            CvInvoke.Rectangle(currentFrameReconocimiento, face, new Bgr(Color.Green).MCvScalar, 2);

                        }
                        else
                        {
                            CvInvoke.PutText(currentFrameReconocimiento, "Unknown", new Point(face.X - 2, face.Y - 2),
                                FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                            CvInvoke.Rectangle(currentFrameReconocimiento, face, new Bgr(Color.Red).MCvScalar, 2);

                        }
                    }
                }
                //Render the video capture into the Picture Box picCapture
                picReconocimiento.Image = currentFrameReconocimiento.Bitmap;
            }

            if(recognizerReconocimiento != null)
                recognizerReconocimiento.Dispose();

            //Dispose the Current Frame after processing it to reduce the memory consumption.
            if (currentFrameReconocimiento != null)
                currentFrameReconocimiento.Dispose();
        }

        public Image<Bgr, Byte> imageToEmguImage(System.Drawing.Image imageIn)
        {
            Bitmap bmpImage = new Bitmap(imageIn);
            Emgu.CV.Image<Bgr, Byte> imageOut = new Emgu.CV.Image<Bgr, Byte>(bmpImage);

            return imageOut;
        }



        private void btnReconocimiento_Click(object sender, EventArgs e)
        {
            if (videoCapture != null) videoCapture.Dispose();
            videoCapture = new Capture();
            Application.Idle += ReconocimientoFacial;
        }
    }
}

