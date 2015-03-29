/*******************************************************************************
 * Copyright (c) 2010-2014, Benedek Izso, Gabor Szarnyas, Istvan Rath and Daniel Varro
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *   Benedek Izso - initial API and implementation
 *   Gabor Szarnyas - initial API and implementation
 *   Dennis Wagelaar
 *******************************************************************************/
package hu.bme.mit.trainbenchmark.ttc.benchmark.atl.benchmarkcases;

import hu.bme.mit.trainbenchmark.ttc.benchmark.emf.EMFBenchmarkCase;
import hu.bme.mit.trainbenchmark.ttc.railway.RailwayPackage;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

import org.eclipse.emf.common.util.URI;
import org.eclipse.emf.ecore.resource.ResourceSet;
import org.eclipse.emf.ecore.resource.impl.ResourceSetImpl;
import org.eclipse.m2m.atl.emftvm.EmftvmFactory;
import org.eclipse.m2m.atl.emftvm.EmftvmPackage;
import org.eclipse.m2m.atl.emftvm.ExecEnv;
import org.eclipse.m2m.atl.emftvm.Field;
import org.eclipse.m2m.atl.emftvm.Metamodel;
import org.eclipse.m2m.atl.emftvm.Model;
import org.eclipse.m2m.atl.emftvm.impl.resource.EMFTVMResourceFactoryImpl;
import org.eclipse.m2m.atl.emftvm.util.DefaultModuleResolver;
import org.eclipse.m2m.atl.emftvm.util.ModuleResolver;

public abstract class ATLBenchmarkCase extends EMFBenchmarkCase {

	public static final String QUERY_PATH = ATLBenchmarkCase.class.getResource(
			"/queries").toString();
	public static final String TRANSFORMATION_PATH = ATLBenchmarkCase.class.getResource(
			"/transformations").toString();

	protected ExecEnv queryExecEnv;
	protected ExecEnv transformExecEnv;
	protected ModuleResolver queryMr;
	protected ModuleResolver transformationMr;

	/**
	 * {@inheritDoc}
	 * 
	 * Initialises ATL/EMFTVM, creates new {@link ExecEnv}s, and registers the
	 * Railway metamodel.
	 */
	@Override
	protected void init() throws IOException {
		super.init();

		final ResourceSet rs = new ResourceSetImpl();
		rs.getResourceFactoryRegistry().getExtensionToFactoryMap()
				.put("emftvm", new EMFTVMResourceFactoryImpl());

		queryMr = new DefaultModuleResolver(URI.createURI(QUERY_PATH + "/")
				.toString(), rs);

		transformationMr = new DefaultModuleResolver(URI.createURI(
				TRANSFORMATION_PATH + "/").toString(), rs);

		final Metamodel railway = EmftvmFactory.eINSTANCE.createMetamodel();
		railway.setResource(RailwayPackage.eINSTANCE.eResource());

		queryExecEnv = EmftvmFactory.eINSTANCE.createExecEnv();
		queryExecEnv.registerMetaModel("RAILWAY", railway);

		transformExecEnv = EmftvmFactory.eINSTANCE.createExecEnv();
		transformExecEnv.registerMetaModel("RAILWAY", railway);
	}

	/**
	 * {@inheritDoc}
	 * 
	 * Loads and registers the input model with the {@link ExecEnv}s.
	 */
	@Override
	public void read() throws IOException {
		super.read();
	
		final Model model = EmftvmFactory.eINSTANCE.createModel();
		model.setResource(resource);
	
		queryExecEnv.registerInputModel("IN", model);
		transformExecEnv.registerInOutModel("IN", model);
	}

	/**
	 * {@inheritDoc}
	 * 
	 * Runs the ATL/EMFTVM matching query.
	 */
	@Override
	protected Collection<Object> check() {
		matches = new ArrayList<>();
	
		final List<?> result = (List<?>) queryExecEnv.run(null);
		matches.addAll(result);
	
		return matches;
	}

	/**
	 * {@inheritDoc}
	 * 
	 * Runs the ATL/EMFTVM repair transformation on the given matches.
	 * 
	 * @param matches
	 *            the matches to transform
	 * @param nElementsToModify
	 *            the number of elements that should be modified by the
	 *            transformation
	 */
	@Override
	protected void modify(final Collection<Object> matches, final long nElementsToModify) {
		final Field matchesField = transformExecEnv.findStaticField(EmftvmPackage.eINSTANCE.getExecEnv(), "matches");
		matchesField.setStaticValue(matches);
		
		transformExecEnv.run(null);
	}

}
