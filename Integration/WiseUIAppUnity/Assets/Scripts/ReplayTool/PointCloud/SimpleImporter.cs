using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;


namespace PointCloudExporter
{
	public class SimpleImporter
	{
		enum DataType { __Float, __Double };

		// Singleton
		private static SimpleImporter instance;
		private SimpleImporter() { }
		public static SimpleImporter Instance {
			get {
				if (instance == null) {
					instance = new SimpleImporter();
				}
				return instance;
			}
		}
		
		public MeshInfos Load(string filePath, int maximumVertex = 65000, float fScale = 1.0f)
		{

			MeshInfos data = new MeshInfos();
			int levelOfDetails = 1;
			if (File.Exists(filePath)) {
				using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open))) {
					int cursor = 0;
					int length = (int)reader.BaseStream.Length;
					string lineText = "";
					bool header = true;
					int vertexCount = 0;
					int colorDataCount = 3;
					int index = 0;
					int step = 0;
                    int normalDataCount = 0;
					DataType dataType = DataType.__Float;
					
					//문제 1.
					//Data Type에 따른 파씽 처리가 제대로되어 있지 않다. 
					//예를 들어 {x,y,z}, {nx, ny, nz}, 에 대한 type이 파일마다 다를 수 있는데,
					//현재 헤더에 명시된 property [type] x을 기준으로 x,y,z, nx, ny,nz의 read type을 결정하기 때문에 
					//에러가 발생할 수 있다. 20201008 IbJeon.

					//문제 2.
					//PC의 좌표계 정립이 되어 있지 않다. 현재 x y,z를 읽을 때 z에 -1을 곱하도록 되어있다.
					

					while (cursor + step < length) {
						if (header) {
							char v = reader.ReadChar();
							if (v == '\n') {
								if (lineText.Contains("end_header")) {
                                    header = false;
								} else if (lineText.Contains("element vertex")) {
									string[] array = lineText.Split(' ');
									if (array.Length > 0) {
										int subtractor = array.Length - 2;
										vertexCount = Convert.ToInt32 (array [array.Length - subtractor]);

										if (vertexCount > maximumVertex) {
											levelOfDetails = 1 + (int)Mathf.Floor(vertexCount / maximumVertex);
											vertexCount = maximumVertex;
										}
										data.vertexCount = vertexCount;
										data.vertices = new Vector3[vertexCount];
										data.normals = new Vector3[vertexCount];
										data.colors = new Color[vertexCount];
									}
								} else if (lineText.Contains("property uchar alpha")) {
									colorDataCount = 4;
                                } else if (lineText.Contains("property float n") || lineText.Contains("property double n")) {
                                    normalDataCount += 1;
                                }
								else if(lineText.Contains("property double x")){ //property double x 만을 보고 버텍스 및 노말 타입을 정하기 때문에 문제의 소지가 있다.
									dataType = DataType.__Double; 
                                }

								lineText = "";
							} else {
								lineText += v;
							}
							step = sizeof(char);
							cursor += step;
						} 
						else 
						{
							if (index < vertexCount) 
							{
								if(dataType == DataType.__Float)
                                {
									float px = -reader.ReadSingle();
									float py = reader.ReadSingle();
									float pz = reader.ReadSingle();

									data.vertices[index] = new Vector3(px, py, -pz) * fScale;
									if (normalDataCount == 3)
									{
										float nx = -reader.ReadSingle();
										float ny = reader.ReadSingle();
										float nz = reader.ReadSingle();

										data.normals[index] = new Vector3(nx, ny, -nz);
									}
									else
									{
										data.normals[index] = new Vector3(1f, 1f, 1f);
									}
									data.colors[index] = new Color(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, 1f);

									step = sizeof(float) * 6 * levelOfDetails + sizeof(byte) * colorDataCount * levelOfDetails;
								}
								else if(dataType == DataType.__Double)
                                {
									double px = -reader.ReadDouble();
									double py = reader.ReadDouble();
									double pz = reader.ReadDouble();

									data.vertices[index] = new Vector3((float)px, (float)py, (float)pz) * fScale;

									if (normalDataCount == 3)
									{
										double nx = -reader.ReadDouble();
										double ny = reader.ReadDouble();
										double nz = reader.ReadDouble();

										data.normals[index] = new Vector3((float)nx, (float)ny, (float)nz);
									}
									else
									{
										data.normals[index] = new Vector3(1f, 1f, 1f);
									}
									data.colors[index] = new Color(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, 1f);

									step = sizeof(double) * 6 * levelOfDetails + sizeof(byte) * colorDataCount * levelOfDetails;
								}

								cursor += step;

								//아래의 경우 무시한다.
								if (colorDataCount > 3) 
								{
									reader.ReadByte();
								}

								if (levelOfDetails > 1)
								{
									for (int l = 1; l < levelOfDetails; ++l)
									{
										for (int f = 0; f < 3 + normalDataCount; ++f)
										{
											reader.ReadSingle();
										}
										for (int b = 0; b < colorDataCount; ++b)
										{
											reader.ReadByte();
										}
									}
								}
								
								++index;
							}
						}
					}
				}
			}
			return data;
		}
	}
}
